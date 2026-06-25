---
baseline_commit: 7c5523101727b24cd989765c5d8e06c9a8704d19
---

# Story 2.2: Add User to Tenant with Explicit Role

Status: done

<!-- Note: Created by the BMAD create-story workflow for Story 2.2. -->

## Story

As an authorized tenant administrator,
I want to add a user directly to a tenant with an explicit role,
so that tenant access can be granted without invitation or pending-member ambiguity.

## Acceptance Criteria

1. Given an authorized user opens the add-member flow from the tenant member table, when the form renders, then it requires a caller-supplied user id and an explicit role of `TenantOwner`, `TenantContributor`, or `TenantReader`, and it does not offer invitation, pending-member, bulk-add, or global Users navigation behavior.
2. Given validation, freshness, authorization reflection, tenant lifecycle, command lifecycle support, and the one-at-a-time command policy are eligible, when the add-member command is submitted, then the browser calls only the server-side command gateway, the gateway submits the existing `AddUserToTenant` domain command through `POST /api/v1/commands`, and a client-generated `messageId` ULID is used as the idempotency key.
3. Given the add-member command has been accepted, when status polling, SignalR projection notifications, or manual refresh occur, then SignalR is treated only as a freshness nudge, the UI re-queries the authoritative tenant detail/member projection, and success is shown only after the re-query proves the target user is present with the requested role.
4. Given the target user is already a member, when the backend returns `UserAlreadyInTenantRejection`, then the command lifecycle shows a rejected state with safe localized text, and the UI does not treat the response as a NoOp, does not render Success styling/copy/announcement, and does not create duplicate optimistic member-row state.
5. Given the requested role is unavailable, `TenantRole.Unknown`, out of range, or would violate authorization rules, when the user attempts to submit or the backend rejects the command, then the flow fails closed with inline validation or an unavailable-action reason, and `RoleEscalationRejection`, `InsufficientPermissionsRejection`, `TenantDisabledRejection`, and `TenantNotFoundRejection` are surfaced as safe localized lifecycle text where returned.
6. Given another command is in flight, the command surface is unavailable, freshness is stale or unknown, authorization is indeterminate, or confirmation is unknown, when the user tries to submit add-member again, then the command trigger is unavailable with a visible inline reason, focus remains recoverable, and duplicate submission does not create optimistic member state.
7. Given the add-member outcome is request sent, accepted, projection pending, confirmed, rejected, failed, degraded, unable to verify, audit pending, audit unavailable, or missing support, when the lifecycle panel renders, then each state remains visible, accessible, localized, and not collapsed into a single success state.
8. Given command lifecycle copy, validation copy, role labels, unavailable reasons, and audit handoff copy are rendered, then all strings use Tenants-owned `.resx` resources with EN/FR parity, whole-string keys with named placeholders where applicable, and no runtime sentence-fragment assembly.
9. Given support-safe output is shown or copied, then raw command payloads, problem details, tokens, decoded JWT contents, stack traces, EventStore metadata, cursor values, internal correlation ids, and raw rejection payloads are never rendered, logged, or copied.
10. Given verification is run, then unit/component tests cover explicit user id and role validation, command gateway submission, `UserAlreadyInTenantRejection`, role rejection paths, one-at-a-time locking, projection-confirmed success, no optimistic member row, SignalR-as-nudge behavior, audit handoff, keyboard complete-or-exit behavior, focus return, live-region politeness, forced-colors-safe state, stable selectors, and resource parity.

## Tasks / Subtasks

- [x] Extend the existing Tenants command gateway for add-member (AC: 2, 4, 5, 9)
  - [x] Add an `AddUserToTenantAsync` method to `ITenantCommandGateway` and `TenantCommandGateway`; do not create a second generic command gateway.
  - [x] Submit `AddUserToTenant` as a `SubmitCommandRequest` with `Tenant = "system"`, `Domain = "tenants"`, `AggregateId = tenantId`, `CommandType = nameof(AddUserToTenant)`, and `Payload = JsonSerializer.SerializeToElement(new AddUserToTenant(tenantId, userId, role))`.
  - [x] Use the already-registered `IUlidFactory.NewUlid()` for the command `MessageId`; never parse or generate `TenantId` or `UserId` as GUIDs or ULIDs.
  - [x] Keep status lookup correlation-id based as Story 2.1 implemented, and keep the `MessageId` available for idempotency/support-safe internal tracking without rendering it by default.
  - [x] Map `UserAlreadyInTenantRejection`, `RoleEscalationRejection`, `InsufficientPermissionsRejection`, `TenantDisabledRejection`, and `TenantNotFoundRejection` to safe bounded lifecycle messages and rejection codes.
  - [x] Preserve current `CreateTenantAsync` behavior and tests while adding the new command path.

- [x] Add add-member lifecycle state that reuses the Story 2.1 command model pattern (AC: 3, 4, 6, 7)
  - [x] Add focused models under `src/Hexalith.Tenants.UI/State/TenantCommands/`, for example `AddUserToTenant` and `TenantAddMemberCommandSnapshot`, or generalize `TenantCreateCommandSnapshot` only if that reduces duplication without weakening create-tenant tests.
  - [x] Store tenant id, target user id, requested role, message id, correlation id, safe message, rejection code, audit handoff state, and focus target.
  - [x] Preserve the non-collapse invariant: request sent, accepted, projection pending, confirmed, rejected, failed, degraded, unable to verify, audit pending, audit unavailable, and missing support remain distinct.
  - [x] Keep last-confirmed member projection separate from submitted intent; never add the target user to the member table until an authoritative tenant detail/member re-query proves the user and role.
  - [x] Treat SignalR as a nudge that can request a re-query but cannot confirm success or audit availability.
  - [x] Keep `Accepted` and `ProjectionPending` distinct when a re-query has no member evidence, matching the Story 2.1 review fix.

- [x] Build the add-member flow from the tenant member table context (AC: 1, 5, 6, 7, 8)
  - [x] Add a focused component under `src/Hexalith.Tenants.UI/Components/Tenants/Members/`, for example `AddTenantMemberFlow.razor` plus CSS, and compose it from `MemberAccessReview` or the tenant detail member section.
  - [x] Use the existing tenant detail projection as context: tenant id, status, freshness, current members, owner count, and surface kind.
  - [x] Required fields are literal user id and explicit role. Do not trim, normalize, case-fold, lookup by email, invite by email, offer pending state, or create global Users navigation.
  - [x] Offer only assignable roles: `TenantOwner`, `TenantContributor`, and `TenantReader`; never offer `Unknown`.
  - [x] Render inline unavailable reasons before submit when authorization, freshness, tenant lifecycle, command gateway, lifecycle support, or one-at-a-time admission is unavailable.
  - [x] Keep change-role and remove-member actions visible but unavailable until Stories 2.3 and 2.4 implement them; do not silently remove their reason slots.
  - [x] Add stable selectors such as `tenants-add-member-flow`, `tenants-add-member-user-id`, `tenants-add-member-role`, `tenants-add-member-submit`, `tenants-add-member-unavailable-reason`, `tenants-add-member-lifecycle`, `tenants-add-member-state`, `tenants-add-member-audit`, and `tenants-add-member-refresh`.

- [x] Implement projection-confirmed member refresh (AC: 3, 6, 7)
  - [x] After command acceptance, run status lookup and tenant detail/member refresh; confirmation is earned only when `GetTenantQuery` or the existing member projection shows the target `UserId` with the requested `TenantRole`.
  - [x] If command status is terminal/completed but the member projection does not show the target role yet, keep `projection pending`; do not show Success.
  - [x] If status lookup fails, SignalR is disconnected, or projection confirmation cannot be proven, show `unable to verify` with retry status lookup / refresh / continue read-only recovery.
  - [x] Refresh the tenant detail/member table without losing the existing detail route, back link, read-only overview state, configuration view, or member table safety context.
  - [x] Render audit handoff as `audit pending`, `audit unavailable`, or `missing support` until Epic 5 audit evidence surfaces are implemented; do not claim an audit receipt exists in this story.

- [x] Add safe rejection, support-safety, localization, accessibility, and responsive handling (AC: 4, 5, 7, 8, 9)
  - [x] Add EN/FR resource parity for `Tenants.AddMember.*` keys covering labels, help text, role labels, validation, unavailable reasons, lifecycle states, rejection text, audit handoff, and recovery actions.
  - [x] Use whole-string localized messages with named placeholders for tenant id, user id, role, and state labels where needed.
  - [x] Keep live-region politeness driven by state: polite for submitted/accepted/projection-pending/confirmed, assertive for rejected/failed/degraded/unable-to-verify/blocked.
  - [x] Ensure keyboard users can complete or exit the flow, validation moves focus to the failed field, blocked/rejected/unknown states move focus to a recoverable lifecycle region, and focus returns to the launching add-member control when the flow closes or completes.
  - [x] Preserve no-color-only and forced-colors behavior with icon/shape/text for every lifecycle/status state.
  - [x] At narrow widths, keep tenant identity, target user id, requested role, lifecycle state, unavailable reason, and recovery action visible or fail closed with a visible reason.
  - [x] Reuse the Story 1.8 support-safe discipline; do not render or copy raw command internals.

- [x] Add focused tests and evidence updates (AC: 1-10)
  - [x] Extend `TenantCommandGatewayTests` or add an add-member gateway test file proving submit request shape, ULID-shaped message id, literal tenant/user ids, role serialization by name, returned correlation id capture, and safe rejection mappings.
  - [x] Add state/model tests for non-collapse lifecycle states, SignalR nudge only, projection-confirmed member evidence, no optimistic row mutation, one-at-a-time blocking, duplicate-submit recovery, and unknown confirmation.
  - [x] Add component tests for field validation, role options excluding `Unknown`, stale/unknown/disabled/unauthorized fail-closed reasons, keyboard/focus behavior, live-region politeness, forced-colors-safe state semantics, and stable selectors.
  - [x] Extend `TenantDetailSurfaceTests` / `MemberAccessReview` tests so adding the add-member flow does not break table headers, row relationships, action reason slots, or read-only change/remove availability.
  - [x] Add resource parity assertions for all new EN/FR keys.
  - [x] Update `tests/test-summary.md` if implementation verification summaries are maintained for this story.
  - [x] Use per-project test execution. If `dotnet test` hits the known .NET 10 Microsoft.Testing.Platform/VSTest issue, use the xUnit v3 in-process executable fallback and record the command.

## Dev Notes

### Story Source And Epic Context

- Story source is Epic 2 Story 2.2. Epic 2 introduces tenant/member mutation flows over the Epic 1 read foundation and Story 2.1 command foundation. [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.2: Add User to Tenant with Explicit Role`]
- FR10 requires a direct add by caller-supplied user id with an explicit role. There is no invitation, pending-member, email-link, or bulk-add step in v1. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-10`; `_bmad-output/planning-artifacts/epics.md#Story 2.2: Add User to Tenant with Explicit Role`]
- Adding an existing member is a business rejection (`UserAlreadyInTenantRejection`), not a NoOp. `ChangeUserRole` handles same-role NoOp in Story 2.3; do not import that behavior into add-member. [Source: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-10`; `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- Story 2.2 should reuse Story 2.1's command gateway/lifecycle foundation. The command story sizing guardrail allows this story to remain a single story because Story 2.1 implemented command gateway, lifecycle model, one-at-a-time/fail-closed behavior, projection re-query confirmation, localization, and ready-gate evidence patterns. [Source: `_bmad-output/planning-artifacts/epics.md#Command Story Sizing Guardrail`; `_bmad-output/implementation-artifacts/2-1-create-tenant-with-projection-confirmed-command-lifecycle.md`]

### Command Contract Details

- `AddUserToTenant` is `public record AddUserToTenant(string TenantId, string UserId, TenantRole Role);`. Commands are plain public records with primary constructors; do not add XML docs, `sealed`, marker interfaces, or new contract fields. [Source: `src/Hexalith.Tenants.Contracts/Commands/AddUserToTenant.cs`; `_bmad-output/project-context.md#C# Language & Contract Rules`]
- `TenantRole` has `Unknown = 0`, `TenantOwner`, `TenantContributor`, and `TenantReader`, serialized by `JsonStringEnumConverter<TenantRole>`. UI role options must exclude `Unknown`; gateway payloads must serialize real role names. [Source: `src/Hexalith.Tenants.Contracts/Enums/TenantRole.cs`]
- `AddUserToTenantValidator` rejects empty tenant id, empty user id, out-of-range roles, and `TenantRole.Unknown`. Keep client validation aligned but still rely on backend validation and domain rejection as the gate. [Source: `src/Hexalith.Tenants.Server/Validators/AddUserToTenantValidator.cs`]
- `TenantAggregate.Handle(AddUserToTenant, ...)` rejects missing tenants, disabled tenants, insufficient permissions, non-assignable roles, and existing members; success emits `UserAddedToTenant`. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs#Handle(AddUserToTenant)`]
- Authorization for add-member is tenant owner or global administrator after membership history exists. The aggregate permits first-user bootstrap on empty tenant history; the UI should not invent a stricter backend invariant, but normal UI add-member availability should still reflect the current tenant detail and authorization result. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs#Handle(AddUserToTenant)`; `docs/tenants-ui-audit-evidence-and-compensating-recovery-spec.md`]
- Identifier casing is significant. `TenantId` and `UserId` are meaningful caller/IdP supplied strings compared with `StringComparer.Ordinal`; do not trim, case-fold, normalize, slugify, or parse them as GUIDs/ULIDs. [Source: `docs/production-auth-claim-contract.md#Identifier Casing Contract`; `_bmad-output/project-context.md#Identity Rules`]
- The safe command request shape for add-member is the same shape documented for corrective access restore: `messageId`, `tenant = "system"`, `domain = "tenants"`, `aggregateId = tenantId`, `commandType = "AddUserToTenant"`, payload with `TenantId`, `UserId`, and explicit `Role`. [Source: `docs/compensating-commands.md#Correction: AddUserToTenant With Explicit TenantRole`]

### Architecture And Boundary Requirements

- Tenants UI is Blazor InteractiveServer with a server-side BFF. The browser must not call backend services directly and must not hold backend access tokens. [Source: `_bmad-output/planning-artifacts/architecture.md#Core Architectural Decisions`; `src/Hexalith.Tenants.UI/Program.cs`]
- All backend command egress goes through `ITenantCommandGateway` / `TenantCommandGateway`, which currently implements create-tenant using `IEventStoreGatewayClient.SubmitCommandAsync` and status lookup at `api/v1/commands/status/{correlationId}`. Extend this path; do not add a browser `HttpClient`, endpoint controller, or generic command infrastructure in Tenants. [Source: `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`; `AGENTS.md#Domain Implementation Boundary`]
- Projection truth is authoritative. Status `Completed` or `EventsPublished` is not proof that the member table changed; confirmation requires a tenant detail/member projection re-query that shows the literal target user id with the requested role. [Source: `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`; `docs/tenants-ui-truth-state-and-action-availability-spec.md#5. Layered Feedback State Set (AC4)`]
- SignalR projection notifications are freshness nudges only. A nudge can trigger re-query/refresh but cannot set `confirmed`, `audit available`, or success. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md#3.4 SignalR is a freshness nudge only`]
- Keep one-at-a-time command policy. Story 2.1 already implemented the narrow Tenants command unavailable/in-flight behavior; Story 2.2 must not introduce toast batching, concurrent command submission, or optimistic success. [Source: `_bmad-output/implementation-artifacts/2-1-create-tenant-with-projection-confirmed-command-lifecycle.md#Senior Developer Review (AI)`; `_bmad-output/planning-artifacts/architecture.md#Process Patterns`]
- The command lifecycle belongs near the affected tenant/member context. Do not turn command lifecycle into navigation and do not use global message bars for row/form-level feedback. [Source: `docs/tenants-ui-truth-state-and-action-availability-spec.md#6. Feedback Placement and Degradation Scope (AC5)`]

### Existing Files To Update And Preserve

- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`: add add-member submission while preserving create-tenant and `GetStatusAsync`.
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`: add `AddUserToTenant` mapping, safe rejection mapping, and tests; preserve create-tenant request/status behavior.
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`: add fail-closed add-member behavior matching the unavailable command surface.
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`: either add focused add-member models in this folder/file or split into clearer files; preserve Story 2.1 tests and state semantics.
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`: current member table renders read-only rows and all action slots as unavailable. Add the add-member entry/flow without breaking table caption, headers, reason catalog, row relationships, copy buttons, change-role/remove-member unavailable slots, or stale/degraded messaging.
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`: current detail page loads `TenantDetailSnapshot` and composes `MemberAccessReview`. Add member refresh/re-query behavior here only if needed; preserve safe back link, overview, configuration view, and current loading/error/stale/degraded states.
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx` and `.fr.resx`: add add-member and lifecycle resources with EN/FR parity.
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`: extend gateway coverage for `AddUserToTenant`.
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs` and related member/detail tests: extend without weakening Epic 1 member table assertions.
- `tests/Hexalith.Tenants.UI.Tests/Components/CreateTenantFlowTests.cs` and `State/TenantCreateCommandSnapshotTests.cs`: use their patterns for lifecycle/focus/resource coverage, but do not couple add-member tests to create-tenant implementation details unnecessarily.

### Scope Boundaries

- Do not add or change backend endpoints, Tenants domain contracts, EventStore server plumbing, DAPR/Aspire wiring, package versions, Dockerfiles, `.sln` files, or submodule files.
- Do not implement change-role, remove-member, edit metadata, tenant lifecycle, configuration, global administrator, audit timeline, audit receipt, or compensating recovery UX in this story.
- Do not create email invitation, pending-member, bulk-add, user search, user creation, or global Users navigation behavior.
- Do not edit events, projections, or state stores to "fix" membership. Corrections are forward commands only.
- Do not expose raw command payloads, backend problem details, command status internals, cursor values, tokens, decoded JWT payloads, stack traces, EventStore metadata, internal correlation ids, or raw rejection payloads.

### Previous Story Intelligence

- Story 2.1 implemented the first Tenants command gateway and lifecycle foundation. Reuse `IEventStoreGatewayClient.SubmitCommandAsync`, `IUlidFactory.NewUlid()`, correlation-id status lookup, safe exception mapping, resource parity, focus recovery, and xUnit v3 component/gateway test patterns. [Source: `_bmad-output/implementation-artifacts/2-1-create-tenant-with-projection-confirmed-command-lifecycle.md`]
- Story 2.1 senior review fixed a state-collapse issue: a projection re-query with no evidence must not turn every accepted state into projection pending. Preserve that distinction for add-member. [Source: `_bmad-output/implementation-artifacts/2-1-create-tenant-with-projection-confirmed-command-lifecycle.md#Senior Developer Review (AI)`]
- Story 2.1 intentionally has no automatic command-status polling / SignalR subscription; lifecycle progresses on refresh/manual trigger while modelling SignalR as nudge-only. Story 2.2 may continue that narrow pattern unless a shared FrontComposer realtime command integration is already available. [Source: `_bmad-output/implementation-artifacts/2-1-create-tenant-with-projection-confirmed-command-lifecycle.md#Senior Developer Review (AI)`]
- Story 1.7 established member table semantics, owner count/status/freshness context, and visible unavailable action reasons. Story 2.2 must turn only add-member into an actionable flow and preserve the read-only safety context around other member actions. [Source: `_bmad-output/implementation-artifacts/1-7-tenant-member-table-and-action-availability.md`; `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`]
- Story 1.8 established support-safe copy behavior and the rule that visible/copyable identifiers must never expose payloads, tokens, stack traces, EventStore metadata, cursor values, or unsafe support references. Apply the same posture to command references. [Source: `_bmad-output/implementation-artifacts/1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md`; `src/Hexalith.Tenants.UI/Services/SupportSafety/SupportSafeCopyClassifier.cs`]
- Current verification pattern: run Release builds and per-project tests. If `dotnet test` hits the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility, use the xUnit v3 in-process executable fallback and record broader unrelated Server/AppHost failures as out-of-scope only when they are genuinely unrelated. [Source: `_bmad-output/implementation-artifacts/2-1-create-tenant-with-projection-confirmed-command-lifecycle.md#Debug Log References`]

### Git Intelligence

- Recent commits are story-scoped Conventional Commits, most recently `feat(story-2.1): Create Tenant with Projection-Confirmed Command Lifecycle`. If this story is committed later, use a Conventional Commit such as `feat(story-2.2): Add User to Tenant with Explicit Role`. [Source: `git log --oneline -5`]
- Recent story changes stayed focused to story artifacts, sprint status, UI components/resources, gateway/state code, UI tests, and test summary. Keep Story 2.2 similarly focused. [Source: `git show --stat --oneline -5`]

### Latest Technical Information

- Use the repo-pinned local stack and contracts: .NET 10 SDK/package pins from `global.json` and `Directory.Packages.props`, Fluent UI Blazor v5 RC inherited by the UI project, FrontComposer command/fallback contracts, EventStore command contracts, xUnit v3, Shouldly, bUnit, and NSubstitute. Do not introduce package versions or new libraries for Story 2.2. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `_bmad-output/planning-artifacts/architecture.md#Technology Stack & Versions`]
- No external API or package research is required for implementation beyond the pinned local contracts; the risk is contract reuse and truth-state correctness, not selecting a new library.

### Project Structure Notes

- Source changes should stay under `src/Hexalith.Tenants.UI/`, primarily `Components/Tenants/Members/`, `Components/Pages/`, `State/TenantCommands/`, `Services/Gateways/`, `Resources/`, and existing CSS colocated with the affected components.
- Tests should stay under `tests/Hexalith.Tenants.UI.Tests/` with xUnit v3, Shouldly, bUnit, and NSubstitute. Test files remain plural `{Class}Tests.cs`.
- The story should not require changes to `src/Hexalith.Tenants.Contracts`, `src/Hexalith.Tenants.Server`, `src/Hexalith.Tenants`, AppHost, IntegrationTests, submodules, or package metadata unless the existing command contract is proven wrong.

### References

- Story source: `_bmad-output/planning-artifacts/epics.md#Story 2.2: Add User to Tenant with Explicit Role`
- Epic context and command sizing: `_bmad-output/planning-artifacts/epics.md#Epic 2: Tenant Membership and Tenant Record Management`; `_bmad-output/planning-artifacts/epics.md#Command Story Sizing Guardrail`
- PRD: `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#FR-10`; `_bmad-output/planning-artifacts/prds/prd-tenants-2026-06-02/prd.md#Cross-Cutting Interaction Principles`
- Architecture: `_bmad-output/planning-artifacts/architecture.md#Core Architectural Decisions`; `_bmad-output/planning-artifacts/architecture.md#Communication Patterns`; `_bmad-output/planning-artifacts/architecture.md#Process Patterns`; `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`
- Truth-state and evidence specs: `docs/tenants-ui-truth-state-and-action-availability-spec.md#3.4 SignalR is a freshness nudge only`; `docs/tenants-ui-truth-state-and-action-availability-spec.md#5. Layered Feedback State Set (AC4)`; `docs/tenants-ui-accessibility-localization-and-acceptance-evidence-spec.md`
- Command/auth contracts: `src/Hexalith.Tenants.Contracts/Commands/AddUserToTenant.cs`; `src/Hexalith.Tenants.Contracts/Enums/TenantRole.cs`; `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`; `src/Hexalith.Tenants.Server/Validators/AddUserToTenantValidator.cs`; `docs/production-auth-claim-contract.md#Identifier Casing Contract`
- Existing UI implementation: `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`; `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`; `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`; `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- Prior story evidence: `_bmad-output/implementation-artifacts/2-1-create-tenant-with-projection-confirmed-command-lifecycle.md`; `_bmad-output/implementation-artifacts/1-7-tenant-member-table-and-action-availability.md`; `_bmad-output/implementation-artifacts/1-8-support-safe-identifier-copy-and-epic-1-readiness-evidence.md`
- Project rules: `_bmad-output/project-context.md`; `AGENTS.md`

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Create-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Input discovery loaded sprint status, project context, `epics.md`, `architecture.md`, targeted PRD/UX/docs references, Story 2.1, current Tenants UI source files, AddUser domain contracts/aggregate behavior, test/source file inventory, and recent git history.
- Validation was run against `.agents/skills/bmad-create-story/checklist.md`; disaster-prevention checks are reflected in this story context.
- Dev-story activation resolved customization with no prepend/append steps and persistent facts from `_bmad-output/project-context.md`.
- Story 2.2 was marked `in-progress` with baseline commit `7c5523101727b24cd989765c5d8e06c9a8704d19`; sprint status was updated to `in-progress`.
- Implemented add-member command gateway, lifecycle state, member-table add flow, projection-confirmed detail refresh hook, EN/FR AddMember resources, gateway/state/component/resource tests, and supporting existing test double updates.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore --filter TenantCommandGatewayTests` failed before test execution with the known .NET 10 Microsoft.Testing.Platform/VSTest error.
- `dotnet build src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj -c Release` and `--no-restore` failed during MSBuild project-reference target framework negotiation with no compiler diagnostics; diagnostic output showed the failure under `_GetProjectReferenceTargetFrameworkProperties` while resolving `Hexalith.FrontComposer.Contracts`.
- `dotnet workload restore Hexalith.Tenants.slnx` failed because the SDK attempted to write under read-only `/home/administrator/.dotnet/metadata`; retrying with `DOTNET_CLI_HOME=/tmp/dotnet-cli-home TMPDIR=/tmp` still attempted the read-only SDK metadata path.
- `dotnet build Hexalith.FrontComposer/src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj -c Release -f net10.0 --no-restore` passed, narrowing the build blocker to project-reference negotiation/workload resolver behavior rather than that source project.
- xUnit v3 generated runner fallback executed the previously compiled UI test assembly for selected existing classes with `Total: 68, Failed: 0`; requesting the new Story 2.2 test classes returned `Total: 0` because the build blocker prevented recompiling the assembly with new tests.
- Resource validation: EN/FR `Tenants.AddMember.*` and `Tenants.Members.*` key parity checks passed; Python XML parsing of `TenantsResources.resx` and `TenantsResources.fr.resx` passed. `xmllint` was unavailable in this environment.
- Continuation session (Claude Opus 4.8): the earlier "blocked" build was not an environment failure — `dotnet restore`/`build` of `tests/Hexalith.Tenants.UI.Tests` works in this environment. The real blockers were two genuine compilation errors and two nullable-warnings-as-errors in the prior uncompiled code.
- Fixed CS0118 in `TenantCreateCommandModels.cs`: the bare name `TenantDetail` resolved to the sibling namespace `Hexalith.Tenants.UI.State.TenantDetail` instead of the query DTO. Replaced the namespace import with `using TenantDetailProjection = Hexalith.Tenants.Contracts.Queries.TenantDetail;` plus `using TenantDetailSnapshot = ...State.TenantDetail.TenantDetailSnapshot;` and aliased the two `TenantAddMemberCommandSnapshot` member-projection usages.
- Fixed CS8604 (nullable-as-error) in `TenantCommandGatewayTests.cs` add-member validation tests by asserting `result.SafeMessage.ShouldNotBeNull().ShouldContain(...)`.
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore` then passed with 0 warnings and 0 errors.
- `dotnet test` reproduced the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility (`Testing with VSTest target is no longer supported ... on .NET 10 SDK`). Used the xUnit v3 in-process executable fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests`.
- First fallback run surfaced one genuine test failure: `Add_user_to_tenant_maps_safe_rejection_text(InsufficientPermissionsRejection)` returned `Failed` because `SafeAddMemberRejection` did not recognize `InsufficientPermissions` for a 409-carried domain rejection. Added the `InsufficientPermissions` reason mapping in `TenantCommandGateway.cs`; rerun passed 200/200.
- QA generate-e2e-tests workflow ran for Story 2.2 using existing xUnit v3/bUnit test infrastructure. Auto-applied discovered coverage gaps: add-member status lookup now covers `InsufficientPermissionsRejection`; add-member component tests now cover tenant lifecycle/command-surface fail-closed behavior and duplicate-submit in-flight blocking.
- `dotnet build tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-restore -warnaserror -m:1 -nr:false` passed with 0 warnings and 0 errors.
- `dotnet test tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj -c Release --no-build -m:1 -nr:false` reproduced the known .NET 10 Microsoft.Testing.Platform/VSTest incompatibility; fallback `tests/Hexalith.Tenants.UI.Tests/bin/Release/net10.0/Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` passed 205/205.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story context scopes Story 2.2 to direct add-member with explicit role, reusing Story 2.1 command gateway/lifecycle foundations.
- Story context identifies the key implementation risks: do not treat `UserAlreadyInTenantRejection` as NoOp, do not offer `TenantRole.Unknown`, do not optimistically add member rows, and do not show success before projection re-query proves the user and role.
- Story context names concrete existing files to update and preservation requirements for member table semantics, create-tenant command behavior, support-safety, localization, accessibility, and tests.
- Implementation added `AddUserToTenantAsync` to the existing Tenants command gateway using `IUlidFactory.NewUlid()` for `MessageId`, `Tenant = "system"`, `Domain = "tenants"`, `AggregateId = tenantId`, `CommandType = nameof(AddUserToTenant)`, and literal tenant/user ids.
- Implementation added safe add-member rejection mappings for `UserAlreadyInTenant`, `RoleEscalation`, `InsufficientPermissions`, `TenantDisabled`, and `TenantNotFound` without rendering raw payloads, internal correlation ids, tokens, or backend details.
- Implementation added focused add-member lifecycle state that keeps submitted intent separate from confirmed projection evidence and treats SignalR as a nudge only.
- Implementation added `AddTenantMemberFlow` under the member table context with literal user id input, explicit assignable role selection, fail-closed unavailable reasons, lifecycle/audit/recovery selectors, projection-confirmed refresh behavior, and no optimistic member-row mutation.
- Implementation added EN/FR AddMember resources and tests for gateway request shape/rejections, state non-collapse, projection evidence, component validation/rejection/fail-closed behavior, stable selectors, and resource parity.
- Continuation session resolved the prior "blocked" state: the UI and test projects build cleanly (Release, warnings-as-errors), and the full `Hexalith.Tenants.UI.Tests` suite passes 200/200 (0 failed, 0 skipped) via the xUnit v3 executable fallback after fixing two compile errors, two nullable-as-error test issues, and one genuine `InsufficientPermissions` rejection-mapping gap.
- QA generate-e2e-tests added focused Story 2.2 tests for add-member status `InsufficientPermissionsRejection`, command-surface and tenant-lifecycle fail-closed behavior, and duplicate-submit in-flight blocking; the full UI test suite now passes 205/205 via the xUnit v3 executable fallback.
- All 10 acceptance criteria are covered by passing tests (gateway request shape/ULID/literal ids/role-by-name/rejection mapping; lifecycle non-collapse and projection-confirmed evidence; component validation/role-options/fail-closed/focus/live-region/selectors; member-table preservation; EN/FR resource parity), so Story 2.2 is honestly marked `review` under the BMAD Definition of Done.

### File List

- `_bmad-output/implementation-artifacts/2-2-add-user-to-tenant-with-explicit-role.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Hexalith.Tenants.UI/Components/Pages/TenantDetailPage.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/AddTenantMemberFlow.razor.css`
- `src/Hexalith.Tenants.UI/Components/Tenants/Members/MemberAccessReview.razor`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.fr.resx`
- `src/Hexalith.Tenants.UI/Resources/TenantsResources.resx`
- `src/Hexalith.Tenants.UI/Services/Gateways/ITenantCommandGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/TenantCommandGateway.cs`
- `src/Hexalith.Tenants.UI/Services/Gateways/UnavailableTenantCommandGateway.cs`
- `src/Hexalith.Tenants.UI/State/TenantCommands/TenantCreateCommandModels.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/AddTenantMemberFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/CreateTenantFlowTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Components/TenantDetailSurfaceTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/Services/Gateways/TenantCommandGatewayTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/State/TenantAddMemberCommandSnapshotTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsUiCompositionTests.cs`
- `tests/Hexalith.Tenants.UI.Tests/TenantsWorkspaceTests.cs`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `tests/test-summary.md`

### Change Log

- 2026-06-06T05:08:08+02:00 - Implemented Story 2.2 add-member gateway, lifecycle model, member-table flow, projection refresh hook, localization, and focused tests; validation is blocked by local .NET workload/MSBuild project-reference failure before new tests can compile.
- 2026-06-06 - Continuation session (Claude Opus 4.8) completed Story 2.2: fixed the `TenantDetail` namespace/type collision (CS0118) in `TenantCreateCommandModels.cs`, two nullable-as-error assertions in `TenantCommandGatewayTests.cs`, and added the missing `InsufficientPermissions` add-member rejection mapping in `TenantCommandGateway.cs`. UI + test projects build clean (Release, warnings-as-errors); full `Hexalith.Tenants.UI.Tests` suite passes 200/200 via the xUnit v3 executable fallback. Updated `tests/test-summary.md`. Status moved in-progress → review.
- 2026-06-06 - QA generate-e2e-tests workflow added focused Story 2.2 coverage gaps and updated test summaries; UI test build passes and xUnit v3 fallback passes 205/205.
- 2026-06-06 - Adversarial code review (story-automator-review) auto-fixed two support-safety/correctness defects in the shared command status path (`TenantCommandGateway.cs`) and added two regression tests (`TenantCommandGatewayTests.cs`): the generic rejected fallback is now command-neutral, and the failure-reason redaction filter now also strips bearer/jwt/cursor/etag markers. UI test project builds clean (Release, warnings-as-errors) and the xUnit v3 fallback passes 210/210. Status moved review → done.

## Senior Developer Review (AI)

**Reviewer:** Administrator
**Date:** 2026-06-06
**Outcome:** Approve (auto-fix applied)

### Scope

Adversarial validation of every Acceptance Criterion and every `[x]` task against the actual implementation and git reality. Reviewed source: `TenantCommandGateway.cs`, `ITenantCommandGateway.cs`, `UnavailableTenantCommandGateway.cs`, `TenantCreateCommandModels.cs`, `AddTenantMemberFlow.razor(.css)`, `MemberAccessReview.razor`, `TenantDetailPage.razor`, EN/FR `.resx`, and the gateway/state/component test files. `_bmad/` and `_bmad-output/` excluded per workflow.

### Verification Evidence

- Build: `dotnet build tests/Hexalith.Tenants.UI.Tests -c Release -warnaserror` → 0 warnings, 0 errors.
- Tests: xUnit v3 in-process fallback `Hexalith.Tenants.UI.Tests -noLogo -noColor -parallel none` → **210/210** passed (205 pre-existing + 5 added this review).
- AC1–AC10: all confirmed implemented and test-covered. No CRITICAL or HIGH findings; no task marked `[x]` was found undone.
- Git vs File List: the only changed-but-undocumented file is `_bmad-output/story-automator/orchestration-1-*.md`, an excluded BMAD orchestration artifact — not a discrepancy.

### Findings and Resolution

- **[MEDIUM — FIXED] Cross-command rejected copy in shared status path.** `GetStatusAsync`/`SafeMessageForStatus` is shared by create-tenant and add-member, but the generic `CommandStatus.Rejected` fallback hardcoded "The create tenant command was rejected." An add-member command rejected with an unrecognized rejection type would surface create-tenant copy in the add-member lifecycle panel. Fixed to command-neutral "The command was rejected." Regression test: `Status_lookup_generic_rejection_stays_command_neutral_for_shared_status_path`.
- **[LOW — FIXED] Incomplete support-safety redaction (AC9 / Story 1.8).** `BoundSafeFailureReason` (now exercised by the add-member status path) filtered `payload`/`token`/`stack`/`correlation` but not the other forbidden categories AC9 enumerates. Extended the marker set to also strip `bearer`/`jwt`/`cursor`/`etag`. Regression test: `Status_lookup_redacts_unsafe_support_markers_in_failure_reason` (Theory).

### Observations (not auto-fixed — out of this story's scope)

- `IsCommandSurfaceAvailable` is not wired from `ITenantsBffComposition.IsCommandSurfaceConnected` in `TenantDetailPage`/`MemberAccessReview` (defaults `true`). This mirrors the existing Story 2.1 `CreateTenantFlow` wiring in `TenantsWorkspace.razor`; the flow still fails closed at submit time when the gateway is the `UnavailableTenantCommandGateway`. Changing only add-member would diverge from the create-tenant baseline, so this is left as a shared follow-up for a future story.
- Each member row still renders an `AddMember` action slot as "Unavailable" alongside the now-actionable `AddTenantMemberFlow`. This is a deliberate Story 1.7 preservation explicitly asserted by `TenantDetailSurfaceTests` (6 action slots for a 2-member table); not changed.
