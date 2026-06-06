---
baseline_commit: a67e9b0
---

# Story 3.1: Tenant Lifecycle Command Availability and Blocked-State Guardrail

Status: done

<!-- Note: Created by the BMAD create-story workflow for Story 3.1. -->

## Story

As an authorized global administrator,
I want to see whether tenant enable or disable actions are available and why they may be blocked,
so that high-impact lifecycle controls never render as casual or falsely available actions.

## Acceptance Criteria

1. Given a tenant detail surface is loaded for a caller who may have global administrator authority, when lifecycle action availability is evaluated, then the UI uses server-side authorization reflection, tenant lifecycle state, freshness, command-surface readiness, and platform-wide governance gate status to determine availability, and indeterminate authorization, unknown freshness, missing lifecycle support, or unresolved high-impact governance fails closed.
2. Given tenant lifecycle control is blocked by platform-wide destructive-action policy, when the enable or disable action slot renders, then the action is unavailable with a visible localized `UnavailableActionReason`, and no lifecycle command can be submitted from the UI.
3. Given the tenant is already active or already disabled, when lifecycle action availability is computed, then same-state commands are represented as unavailable or expected rejection states, and the UI names `TenantLifecycleStateAlreadySet` as the safe localized domain outcome when applicable.
4. Given the tenant projection is stale, degraded, unknown, unauthorized, or disabled, when lifecycle controls are displayed, then the current projection truth remains visible with no optimistic transition, and the UI does not imply the tenant is enabled or disabled until the projection proves it.
5. Given lifecycle controls are high-impact, when the page is viewed on mobile or any viewport that cannot preserve full safety context, then enable and disable actions are unavailable with a visible reason, and safety-critical status, freshness, and tenant identity remain visible.
6. Given this story is complete, when verification is run, then unit/component tests cover authorization reflection, command/governance gate blocking, same-state availability, stale/unknown freshness, mobile fail-closed behavior, and no command submission while blocked.
7. Given accessibility verification is run, then component or Playwright coverage verifies inline reasons, keyboard reachability, forced-colors rendering, live-region behavior, stable selectors such as `data-testid="tenants-lifecycle-unavailable-reason"`, and support-safe blocked-state copy.

## Tasks / Subtasks

- [x] Add a tenant lifecycle availability model without command submission (AC: 1-5)
  - [x] Add a focused availability model under `src/Hexalith.Tenants.UI/State/TenantDetail/` or `src/Hexalith.Tenants.UI/State/TenantCommands/` that evaluates `EnableTenant` and `DisableTenant` separately from command lifecycle submission state.
  - [x] Include current tenant id, current `TenantStatus`, target operation, freshness, detail surface kind, command-surface connection, high-impact governance readiness, authorization reflection state, unavailable reason category, safe message key, focus target, and live-region politeness.
  - [x] Default unresolved platform-wide destructive governance to blocked. Story 3.1 must not make lifecycle commands available by assuming the future Story 3.2 gate is clear.
  - [x] Represent same-state availability explicitly: active tenant blocks `EnableTenant`; disabled tenant blocks `DisableTenant`; safe copy must name `TenantLifecycleStateAlreadySet` as the expected domain rejection, not success or NoOp.
  - [x] Treat `TenantStatus.Unknown`, stale or unknown freshness, `TenantDetailSurfaceKind.Stale`, `Degraded`, `Unknown`, `Unauthorized`, or missing command-surface support as blocked. `aging` or `refreshing` data may remain visible but must not bypass high-impact governance.

- [x] Render a blocked lifecycle action slot on the existing tenant detail surface (AC: 1-5, 7)
  - [x] Add a focused component under `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/`, for example `TenantLifecycleActionAvailability.razor` plus colocated CSS if needed.
  - [x] Compose the component from `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor` near the existing status/lifecycle facts so tenant identity, current lifecycle status, and freshness remain visible together.
  - [x] Render both target operations in the action slot, but keep them disabled while governance is unresolved. Do not add an enabled primary/destructive lifecycle submit button in Story 3.1.
  - [x] Provide visible, hover-free reasons using the canonical unavailable-action categories: `missing permission`, `stale data`, `missing lifecycle support`, and `high-impact flow not ready`.
  - [x] Use stable selectors including `tenants-lifecycle-actions`, `tenants-lifecycle-enable`, `tenants-lifecycle-disable`, `tenants-lifecycle-unavailable-reason`, `tenants-lifecycle-state`, `tenants-lifecycle-current-status`, `tenants-lifecycle-freshness`, and `tenants-lifecycle-governance-gate`.
  - [x] Make blocked controls keyboard reachable where useful for explanation, or render non-interactive disabled action text with a keyboard-reachable reason. Do not rely on tooltip-only copy.

- [x] Use only existing BFF/composition signals and fail closed when authority is indeterminate (AC: 1, 2, 6)
  - [x] Use an existing server/BFF-reflected signal if one is already available for global-administrator authority or command-surface readiness. `ITenantsBffComposition.IsCommandSurfaceConnected` already exists for command-surface availability.
  - [x] If no confirmed global-administrator authorization reflection source exists, keep lifecycle actions blocked with a visible localized `missing permission` or `high-impact flow not ready` reason. Do not infer trusted global-administrator authority solely from client-side claim parsing unless the server/BFF contract is already established and tested.
  - [x] Do not add a new backend authorization endpoint, read endpoint, command status endpoint, controller, browser-side backend client, or generic authorization service in Tenants for this story.
  - [x] Do not change member, metadata, create-tenant, or configuration command eligibility except where the lifecycle component needs shared read-only facts.

- [x] Preserve current projection truth and surrounding detail behavior (AC: 3-5)
  - [x] Keep the existing tenant detail identity, status badge, lifecycle label, freshness badge, metadata edit flow, member access review, configuration view, support-safe copy, stale/degraded messaging, and return navigation behavior unchanged.
  - [x] Do not overwrite `TenantDetail.Status`, list row status, page title, metadata, member state, or configuration state with an in-flight or intended lifecycle value.
  - [x] Treat SignalR and command-surface availability as nudges/readiness inputs only. Story 3.1 has no lifecycle command tracking handle and must not display `accepted`, `confirmed`, or `audit available` for lifecycle operations.
  - [x] On disabled tenants, preserve the disabled projection truth and show that most tenant-scoped commands remain blocked by `TenantDisabled`; lifecycle enable remains blocked by governance until Story 3.2 clears the gate.
  - [x] On narrow/mobile layouts, keep tenant id, tenant name, current lifecycle status, freshness, and unavailable reason visible before any lifecycle command affordance.

- [x] Add Tenants-owned localization and support-safe copy (AC: 2-5, 7)
  - [x] Add EN/FR resource parity under `Tenants.Lifecycle.*` in `TenantsResources.resx` and `TenantsResources.fr.resx`.
  - [x] Use whole-string localized messages with named/positional placeholders consistent with existing resources; do not assemble translated fragments at runtime.
  - [x] Include safe messages for high-impact governance blocked, missing command lifecycle support, missing permission, stale/unknown freshness, already active, already disabled, unknown tenant lifecycle state, and mobile fail-closed mode.
  - [x] Copy must not expose raw command payloads, EventStore metadata, correlation ids, bearer tokens, decoded JWTs, cursors, ETags, stack traces, raw claims, or real PII.
  - [x] Recovery copy should use forward-safe verbs such as `refresh`, `continue read-only`, `request permission`, or `escalate`; never use `undo`, `rollback`, or imply event/state-store edits.

- [x] Add focused unit/component tests (AC: 1-7)
  - [x] Add availability model tests for active/disabled/unknown status, same-state `TenantLifecycleStateAlreadySet` mapping, stale/unknown freshness, degraded/unauthorized surface kind, missing command support, unresolved governance, and indeterminate authorization.
  - [x] Add component tests for `TenantLifecycleActionAvailability` rendering stable selectors, visible unavailable reasons, keyboard-reachable explanation, no command submit path, no success copy, forced-colors/no-color-only markers, and live-region politeness.
  - [x] Extend `TenantDetailSurfaceTests` to verify the lifecycle action slot is composed beside current status/freshness and that existing metadata/member/configuration/detail behavior is preserved.
  - [x] Add tests proving no `EnableTenantAsync`, `DisableTenantAsync`, `SubmitCommand`, or lifecycle command gateway call is reachable while Story 3.1 governance is blocked.
  - [x] Add EN/FR resource parity checks if the repository continues resource-parity assertions in UI tests.
  - [x] Update maintained verification summaries in `_bmad-output/implementation-artifacts/tests/test-summary.md` and `tests/test-summary.md` if this repository continues that practice for Story 3.1.
  - [x] Run per-project verification. Prefer `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` plus the xUnit v3 in-process executable when `dotnet test` hits the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility.

## Dev Notes

### Story Source And Epic Context

- Story source is Epic 3 Story 3.1. Epic 3 is "Tenant Lifecycle and Configuration Control"; Story 3.1 is the lifecycle command availability/readiness guardrail, not the enable/disable command flow itself. [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.1: Tenant Lifecycle Command Availability and Blocked-State Guardrail`]
- FR15 requires disabling/enabling tenants to be global-administrator-only, high-impact, consequence-previewed, and projection-confirmed. Story 3.1 delivers the honest blocked/action-availability surface that precedes Story 3.2. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-15: Disable or enable a tenant`]
- Implementation readiness explicitly states that platform-wide destructive actions remain categorically blocked pending governance/contract confirmation; Story 3.1 is buildable because it renders the readiness/blocked state, while Story 3.2 remains gated. [Source: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-05-v2.md#External Build-Start Gates`]
- The command story sizing guardrail split FR15 into Story 3.1 availability guardrail plus Story 3.2 command flow. Do not collapse them by implementing lifecycle submission in Story 3.1. [Source: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-05-v2.md#Story Sizing & Dependencies`]

### Backend And Domain Contract Facts

- `EnableTenant` and `DisableTenant` already exist as plain public command records with a single `TenantId`. Do not add XML docs, `sealed`, marker interfaces, or new command fields. [Source: `src/Hexalith.Tenants.Contracts/Commands/EnableTenant.cs`; `src/Hexalith.Tenants.Contracts/Commands/DisableTenant.cs`; `_bmad-output/project-context.md#C# Language & Contract Rules`]
- `TenantAggregate.Handle(DisableTenant, ...)` and `Handle(EnableTenant, ...)` require trusted global administrator authority, reject missing tenants, reject same-state requests with `TenantLifecycleStateAlreadySetRejection`, and emit `TenantDisabled` or `TenantEnabled` otherwise. Story 3.1 must reflect these safe outcomes but must not dispatch the commands. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs#Handle(DisableTenant)`; `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs#Handle(EnableTenant)`]
- `TenantLifecycleStateAlreadySetRejection` carries `TenantId`, `CurrentStatus`, `RequestedStatus`, and `CommandName`; duplicate lifecycle attempts are rejections, not NoOps or successes. [Source: `src/Hexalith.Tenants.Contracts/Events/Rejections/TenantLifecycleStateAlreadySetRejection.cs`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#D. Mechanism decisions, rejection/NoOp matrix & rationale`]
- Disabled tenant status is an eventually consistent availability signal. Most member/metadata/configuration commands targeting disabled tenants reject with `TenantDisabledRejection`; lifecycle enable is a separate global-admin operation but remains UI-blocked by governance in Story 3.1. [Source: `docs/compensating-commands.md#Tenant Lifecycle Correction`; `docs/idempotent-event-processing.md`]
- Tenant ids are literal caller-supplied strings. Do not parse, generate, normalize, or case-fold `TenantId` as GUID/ULID. [Source: `_bmad-output/project-context.md#Identity Rules`]

### Architecture And UX Guardrails

- Tenants UI is Blazor InteractiveServer with a server-side BFF. Browser code must not call backend services directly or hold backend access tokens. [Source: `_bmad-output/planning-artifacts/architecture.md#Selected Starter: new src/Hexalith.Tenants.UI Blazor host composing the FrontComposer Shell`]
- Backend egress must stay behind existing BFF/query/command gateways. Story 3.1 should not add lifecycle methods to `ITenantCommandGateway`; that belongs to Story 3.2 when the governance gate is cleared. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`; `AGENTS.md#Domain Implementation Boundary`]
- Current command-surface readiness is exposed by `ITenantsBffComposition.IsCommandSurfaceConnected`, backed by whether the command gateway is not `UnavailableTenantCommandGateway`. If used, wire it only as an availability input and preserve existing command flows. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantsBffComposition.cs`]
- Fail-closed ordering is load-bearing: freshness, authorization, command lifecycle support, preview/readiness, and governance must be eligible before high-impact actions can proceed. Unknown or stale values block. [Source: `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Truth & Honesty Invariants`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#G. Canonical state sets`]
- Unavailable-action reasons are canonical and visible: `missing permission`, `stale data`, `missing lifecycle support`, `missing consequence preview`, `missing audit proof`, and `high-impact flow not ready`. Story 3.1 should use the relevant subset and not invent new reason categories. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#G. Canonical state sets`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#unavailable-action-reason`]
- Command lifecycle state must not be shown as success before projection truth. Story 3.1 has no lifecycle command attempt, so it should not show `accepted`, `projection pending`, `confirmed`, `audit pending`, or `audit available` for lifecycle actions. [Source: `_bmad-output/planning-artifacts/architecture.md#Implementation Patterns & Consistency Rules`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#command-lifecycle-panel`]
- Mobile/narrow high-impact command flows are non-goals for v1; mobile should remain read-only triage/lookup/audit reference for high-impact lifecycle operations. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#13. Non-Goals`]

### Existing Files To Update And Preserve

- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`: currently renders tenant identity, status, lifecycle label, freshness badge, metadata edit, member access review, and configuration read view. Compose the lifecycle availability component here and preserve existing refresh/return/projection evidence behavior.
- `src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor`: existing no-color-only freshness badge with text and icon. Reuse or align with it for current freshness display; do not replace the shared badge in this story.
- `src/Hexalith.Tenants.UI/Components/Tenants/TenantConfigurationView.razor`: already renders command-unavailable copy for stale/unknown configuration preview. Preserve read-only configuration behavior.
- `src/Hexalith.Tenants.UI/Components/Tenants/Metadata/EditTenantMetadataFlow.razor`: current command flow gates disabled/stale/unknown and tracks metadata command activity. Do not regress metadata edit behavior.
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/*.razor`: existing add/change/remove member flows depend on command-surface and tenant lifecycle gates. Do not alter them except for unavoidable compile-safe composition changes.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`: contains shared command lifecycle enums and per-command snapshots. Add lifecycle availability state here only if it genuinely benefits from existing command vocabulary; otherwise prefer a focused detail-state file because Story 3.1 has no lifecycle command submission.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `.fr.resx`: add `Tenants.Lifecycle.*` EN/FR resources with parity.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`: extend detail composition coverage.
- `tests/Hexalith.Tenants.UI.Tests/Components/`: add lifecycle availability component tests.
- `tests/Hexalith.Tenants.UI.Tests/State/`: add availability model tests if a separate model is introduced.

### Scope Boundaries And Anti-Patterns

- Do not implement `EnableTenantAsync`, `DisableTenantAsync`, lifecycle command submission, command polling, projection confirmation, destructive confirmation, or audit handoff in Story 3.1. Those belong to Story 3.2 after the governance gate is cleared.
- Do not add backend endpoints, controllers, browser-side backend clients, generic command buses, shared FrontComposer components, AppHost/Aspire changes, package versions, or technical-module scaffolding in Tenants.
- Do not modify `src/Hexalith.Tenants.Contracts` or `src/Hexalith.Tenants.Server`; lifecycle contracts and aggregate behavior already exist.
- Do not make lifecycle action availability depend on untested client-only authority guesses. If authority is indeterminate, block visibly.
- Do not hide unavailable reasons behind disabled button tooltips. The reason must be inline-visible and accessible.
- Do not show Success, audit proof, command receipt, or a fabricated lifecycle transition for a blocked lifecycle action.
- Do not expose support-unsafe details in labels, logs, announcements, recovery copy, or copy affordances.

### Previous Story Intelligence

- Story 2.5 established the pattern for extending `TenantDetailPage` with a focused command-related component while preserving projection truth and surrounding read behavior. Story 3.1 should follow that narrow composition style but avoid adding a command gateway method. [Source: `_bmad-output/implementation-artifacts/2-5-edit-tenant-metadata-with-safe-validation.md#Existing Files To Update And Preserve`]
- Story 2.5 review found command-surface locking is currently one-directional and broader authorization reflection remains partly unwired in page composition. Story 3.1 should not solve those globally; it should fail closed for lifecycle authority/gate indeterminacy and document the exact lifecycle availability inputs it uses. [Source: `_bmad-output/implementation-artifacts/2-5-edit-tenant-metadata-with-safe-validation.md#Senior Developer Review (AI)`]
- Story 2.4 established destructive-flow preview and honest audit handoff discipline, including no fabricated receipts and no success before projection proof. Story 3.1 only needs blocked-state reuse of that safety posture. [Source: `_bmad-output/implementation-artifacts/2-4-remove-tenant-member-with-consequence-preview.md`]
- Story 1.7 established visible unavailable action reasons for member action availability. Reuse the visible, hover-free reason pattern instead of inventing a new lifecycle-only explanation style. [Source: `_bmad-output/implementation-artifacts/1-7-tenant-member-table-and-action-availability.md`]
- Story 1.8 established support-safe copy behavior. Apply the same support-safety rules to lifecycle blocked-state labels, recovery copy, and test assertions. [Source: `_bmad-output/implementation-artifacts/1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md`]

### Git Intelligence

- Recent commits are story-scoped Conventional Commits: `feat(story-2.5): Edit Tenant Metadata with Safe Validation`, `feat(story-2.4): Remove Tenant Member with Consequence Preview`, `feat(story-2.3): Change Tenant Member Role`, `feat(story-2.2): Add User to Tenant with Explicit Role`, and `feat(story-2.1): Create Tenant with Projection-Confirmed Command Lifecycle`. If Story 3.1 is committed later, use a Conventional Commit such as `feat(story-3.1): Add Tenant Lifecycle Blocked-State Guardrail`. [Source: `git log --oneline -8`]
- There is an unrelated dirty `_bmad-output/story-automator/orchestration-1-20260605-153745.md` file in the worktree during story creation. Do not modify or revert it as part of Story 3.1 implementation. [Source: `git status --short`]

### Latest Technical Information

- Use the repo-pinned local stack and contracts: .NET 10, Fluent UI Blazor v5 RC through the UI project, FrontComposer composition contracts, EventStore command contracts, xUnit v3, Shouldly, bUnit, and NSubstitute. Do not introduce package versions or new libraries for Story 3.1. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`]
- No external API/package research is required for implementation beyond the pinned local contracts. The primary risks are scope creep into Story 3.2, unsafe client-side authority inference, hidden unavailable reasons, optimistic lifecycle status, and support-unsafe copy.

### Project Structure Notes

- Source changes should stay under `src/Hexalith.Tenants.UI/`, primarily `Components/Pages/`, `Components/Tenants/Lifecycle/`, `State/TenantDetail/` or `State/TenantCommands/`, `Services/Gateways/` only if reading `ITenantsBffComposition`, `Resources/`, and colocated CSS.
- Tests should stay under `tests/Hexalith.Tenants.UI.Tests/` with xUnit v3, Shouldly, bUnit, and NSubstitute. Test files remain plural `{Class}Tests.cs`.
- The story should not require changes to `src/Hexalith.Tenants.Contracts`, `src/Hexalith.Tenants.Server`, `src/Hexalith.Tenants`, AppHost, IntegrationTests, submodules, package metadata, or shared technical modules.
- Detected conflict to manage: the domain can execute lifecycle commands today, but planning governance says platform-wide destructive actions are blocked in the UI until Story 3.2 clears the gate. For Story 3.1, governance wins and UI submission remains unavailable.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 3.1: Tenant Lifecycle Command Availability and Blocked-State Guardrail`
- Epic context and requirements: `_bmad-output/planning-artifacts/epics.md#Epic 3: Tenant Lifecycle and Configuration Control`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-15: Disable or enable a tenant`
- Readiness and gates: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-05-v2.md#External Build-Start Gates`; `_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-05-v2.md#Story Sizing & Dependencies`
- PRD addendum: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#B. FrontComposer dependency readiness`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#D. Mechanism decisions, rejection/NoOp matrix & rationale`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/addendum.md#G. Canonical state sets`
- Architecture and UX: `_bmad-output/planning-artifacts/architecture.md#Implementation Patterns & Consistency Rules`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md#Truth & Honesty Invariants`; `_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md#unavailable-action-reason`
- Domain contracts: `src/Hexalith.Tenants.Contracts/Commands/EnableTenant.cs`; `src/Hexalith.Tenants.Contracts/Commands/DisableTenant.cs`; `src/Hexalith.Tenants.Contracts/Events/Rejections/TenantLifecycleStateAlreadySetRejection.cs`; `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`
- Existing UI implementation: `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`; `src/Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor`; `src/Hexalith.Tenants.UI/Services/Gateways/ITenantsBffComposition.cs`; `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- Prior story evidence: `_bmad-output/implementation-artifacts/2-5-edit-tenant-metadata-with-safe-validation.md`; `_bmad-output/implementation-artifacts/2-4-remove-tenant-member-with-consequence-preview.md`; `_bmad-output/implementation-artifacts/1-7-tenant-member-table-and-action-availability.md`; `_bmad-output/implementation-artifacts/1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md`
- Project rules: `_bmad-output/project-context.md`; `AGENTS.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Input discovery loaded sprint status, project context, `epics.md`, `architecture.md`, targeted PRD/addendum/UX/readiness sections, Story 2.5 previous-story intelligence, current Tenants UI detail/gateway/resource/test files, lifecycle domain contracts, and recent git history.
- Validation was run against `.agents/skills/bmad-create-story/checklist.md`; critical disaster-prevention checks are reflected in this story context.
- Dev-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Story 3.1 implementation preserved existing `baseline_commit: a67e9b0`, moved sprint/story status to `in-progress`, and composed lifecycle availability from existing tenant detail and BFF command-surface facts only.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build --no-restore` was attempted and hit the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility; xUnit v3 executable fallback was used.
- Broader non-UI project builds were attempted and blocked by NU1900 because restricted network access prevented NuGet vulnerability data retrieval.
- Server.Tests executable was attempted and still fails in pre-existing documentation/AppHost checks for missing `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` plus stale deployment-readiness evidence; no Story 3.1 source depends on those files.
- QA-generate-e2e-tests activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`; workflow validation used `.agents/skills/bmad-qa-generate-e2e-tests/checklist.md`.
- QA gap pass added focused coverage for disabled tenant enable availability preserving disabled projection truth while governance-blocked, plus aging/refreshing freshness not bypassing unresolved high-impact governance.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story context scopes Story 3.1 to lifecycle action availability and blocked-state rendering only; lifecycle command submission remains Story 3.2.
- Story context identifies the key implementation risks: bypassing platform-wide destructive governance, client-only global-admin inference, hidden disabled reasons, optimistic lifecycle status, support-unsafe copy, and accidental command-gateway expansion.
- Added a focused lifecycle availability model that evaluates enable and disable separately without command submission, command tracking handles, backend endpoints, or command gateway expansion.
- Added a read-only blocked lifecycle action component on tenant detail with stable selectors, visible keyboard-reachable unavailable reasons, current lifecycle status/freshness/governance facts, and unresolved governance/global-admin authority failing closed.
- Added EN/FR `Tenants.Lifecycle.*` resources with parity and support-safe copy for governance, command support, authorization, stale/unknown freshness, same-state, unknown status, and mobile/narrow fail-closed cases.
- Added availability model, component, tenant detail composition, source-safety, responsive/forced-colors, live-region, and resource parity tests for Story 3.1.
- Validation passed for the UI project build and xUnit v3 UI tests; Tier 1 executable tests passed for Contracts, Client, Testing, and Sample projects. Broader Server/AppHost checks retain unrelated pre-existing failures documented in test summaries.
- QA generation added disabled-tenant and aging/refreshing freshness regression coverage; UI test build passed, `dotnet test` reproduced the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility, and xUnit v3 UI tests passed with 340 total, 0 failed.

### File List

- `_bmad-output/implementation-artifacts/3-1-tenant-lifecycle-command-availability-and-blocked-state-guardrail.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Lifecycle/TenantLifecycleActionAvailability.razor.css`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/State/TenantDetail/TenantLifecycleAvailability.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantLifecycleActionAvailabilityTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantLifecycleAvailabilityTests.cs`
- `tests/test-summary.md`

### Change Log

- 2026-06-06T08:10:04+02:00 - Created Story 3.1 context and marked it ready for development.
- 2026-06-06T08:15:08+02:00 - Implemented tenant lifecycle blocked-state availability model, detail action slot, localization, and focused tests; moved story to review.
- 2026-06-06T08:30:20+02:00 - Executed QA-generate-e2e-tests workflow, added disabled projection truth and aging/refreshing governance guard coverage, and updated test summaries.
- 2026-06-06 - Senior Developer Review (AI, auto-fix): verified build (0 warnings) and 340/340 UI tests; fixed dishonest degraded/unavailable/unknown surface reason mapping, resolved live-region role/aria-live contradiction, and hardened primary-availability selection. Status moved to done.

## Senior Developer Review (AI)

**Reviewer:** Administrator (adversarial auto-fix review) · **Date:** 2026-06-06 · **Outcome:** Approve (auto-fixed)

### Verification performed

- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release -m:1 --no-restore` → Build succeeded, 0 warnings, 0 errors.
- xUnit v3 in-process executable (the documented `.NET 10` fallback) → **340 total, 0 failed, 0 skipped** before and after fixes.
- Cross-referenced git working tree against the story File List: all listed source/test files are present and dirty; no undocumented source files and no claimed-but-missing changes. The only extra dirty file is the pre-existing unrelated `_bmad-output/story-automator/orchestration-1-20260605-153745.md`, which the story explicitly says to leave alone.
- EN/FR resource parity confirmed: 33 `Tenants.Lifecycle.*` keys in each `.resx`, 562 total entries each; every key referenced by the component/model exists in both cultures; FR strings are real translations with matching `{0}/{1}/{2}` placeholders.
- Confirmed no command-submission path: no `@onclick`, `<form>`, `type="submit"`, `ITenantCommandGateway`, `EnableTenantAsync`, `DisableTenantAsync`, or `SubmitCommand` reachable from the component.

### AC coverage

- **AC1–AC4** implemented and tested in `TenantLifecycleAvailability` (fail-closed ordering: freshness → surface → unknown status → same-state → narrow → authorization → command surface → governance). Same-state correctly names `TenantLifecycleStateAlreadySet` as an expected rejection (not success/NoOp).
- **AC5** mobile fail-closed reason is modeled and unit/component tested. Note (Low): the page never sets `IsNarrowSafetyContext`, so the *mobile-specific* reason is not user-reachable in production — acceptable because the page hard-blocks every direction anyway (governance `Unresolved` + authorization `Indeterminate`) and mobile high-impact flows are a declared v1 non-goal; wiring viewport JS would exceed story scope.
- **AC6/AC7** covered: stable selectors, visible hover-free reasons, keyboard-reachable reason (`tabindex=0`), forced-colors CSS, live-region politeness, EN/FR parity assertions, and a source-scan test proving no lifecycle command gateway.

### Findings and fixes applied

| # | Severity | Finding | Resolution |
|---|----------|---------|------------|
| 1 | Medium | `Degraded`/`Unavailable`/`Unknown` detail surfaces were mapped to a `missing permission` reason ("server-side global-administrator authority is not proven"), misrepresenting a data-availability problem as an authorization failure — a Truth & Honesty Invariant violation and divergence from the existing membership reason taxonomy (`ProjectionDegraded`/`ProjectionStale`). | Split the mapping in `TenantLifecycleAvailability.cs`: only `Unauthorized` → `MissingPermission`; `Degraded`/`Unavailable`/`Unknown` → `StaleData` (refresh / continue read-only). Updated `TenantLifecycleAvailabilityTests` accordingly. |
| 2 | Medium | Live-region `<div>` combined `role="status"` (implicit `aria-live="polite"`) with a conditional `aria-live="assertive"`, an internally contradictory ARIA construction that produces inconsistent screen-reader behavior; also an outlier versus every sibling live region in the project. | Removed the conflicting `role` and added `aria-atomic="true"` to match the established `aria-live` + `aria-atomic` convention. `aria-live` values (assertive/polite) unchanged. |
| 3 | Low | `PrimaryAvailability` used `.First(IsUnavailable)`, which is safe only because exactly one direction is always same-state-blocked for the current `TenantStatus` set; a new status could make both directions available and throw. | Hardened to `FirstOrDefault(...) ?? Availabilities[0]`. |
| 4 | Low (informational) | In production the page passes `AuthorizationReflection=Indeterminate`, which is evaluated before governance, so the available direction shows the `missing permission` reason rather than the governance "platform gate" copy. | No change — the story explicitly permits either `missing permission` or `high-impact flow not ready`, and authorization-before-governance matches the canonical fail-closed ordering. |

All High/Medium findings were auto-fixed; build and full UI suite remain green (340/340).
