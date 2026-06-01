---
created: 2026-06-01
source_story_key: 8-6-document-compensating-command-patterns
baseline_commit: 1b1ac98
---

# Story 8.6: Document Compensating Command Patterns

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer or operator,
I want clear compensating command guidance,
so that incorrect tenant access changes are corrected explicitly and auditably.

## Acceptance Criteria

1. Given a user is removed from a tenant by mistake, when a developer reads the compensating-command documentation, then the guide explains that recovery is a new explicit command, such as `AddUserToTenant` with a specified role, and it does not describe recovery as hidden undo.
2. Given a compensating action is documented, when the guide walks through an example, then it explains why the intended role must be provided explicitly, and it does not imply the system automatically restores historical roles without a new command.
3. Given compensating command guidance discusses auditability, when a correction is made, then the original event remains in history, and the correction produces its own command outcome and audit event.
4. Given common correction scenarios are documented, when developers review the guide, then it covers mistaken user removal, wrong role assignment, configuration mistake, and tenant lifecycle correction where applicable, and each example identifies the safe command path and expected rejection cases.
5. Given compensating-command docs are validated, when command names, role names, or rejection behavior changes, then examples are checked against current contracts, and stale command snippets or misleading recovery language are corrected.

## Tasks / Subtasks

- [x] Audit and correct the existing compensating-command guide. (AC: 1, 2, 3, 4, 5)
  - [x] Treat `docs/compensating-commands.md` as prior repository state, not accepted story output.
  - [x] Replace any wording that frames compensation as generic "undo" with explicit correction language: a new command changes the current state and appends new history.
  - [x] Keep the existing Sofia wrong-user-removal scenario only if it remains source-backed and support-safe.
  - [x] Preserve the Epic 8 split: Story 8.5 owns timing/eventual-consistency behavior; Story 8.6 owns compensating command intent, examples, auditability, and rejection guidance.

- [x] Rewrite `docs/compensating-commands.md` as the source-backed compensating-command guide. (AC: 1, 2, 3, 4)
  - [x] Define compensating commands as deliberate follow-up commands submitted through `POST /api/v1/commands`, not deletion, rollback, hidden undo, projection editing, event mutation, or direct state-store repair.
  - [x] Explain the event-sourcing rule: stored tenant events are immutable facts; correction appends a new event when the corrective command succeeds.
  - [x] Explain that EventStore command status proves the command outcome, while tenant audit query rows prove successful corrective events after projections catch up.
  - [x] State that rejected compensating commands produce a command rejection outcome, but do not produce the successful corrective audit event.
  - [x] Keep examples support-safe: no raw bearer tokens, decoded JWT payloads, secrets, real tenant/user data, full serialized event payload dumps, or stack traces.

- [x] Document the required correction scenarios with current command paths and rejection cases. (AC: 1, 2, 3, 4)
  - [x] Mistaken user removal: use `AddUserToTenant` with explicit `TenantRole` to restore the wrongly removed user; then use `RemoveUserFromTenant` for the intended user if still required.
  - [x] Explain why the role is explicit: `UserRemovedFromTenant` does not carry role, intervening role changes can make old roles stale, and the operator/developer must choose the intended current role.
  - [x] Wrong role assignment: use `ChangeUserRole` with explicit `NewRole`; same-role requests are `NoOp`, not a new correction event.
  - [x] Configuration mistake: use `SetTenantConfiguration` to overwrite the key with the intended value, or `RemoveTenantConfiguration` if the key should no longer exist.
  - [x] Tenant lifecycle correction: use `EnableTenant` after an accidental `DisableTenant`, or `DisableTenant` after an accidental enablement, with trusted global administrator authority.
  - [x] For each scenario, list expected rejection/no-op cases from current contracts and aggregate behavior: `TenantNotFoundRejection`, `TenantDisabledRejection`, `UserAlreadyInTenantRejection`, `UserNotInTenantRejection`, `RoleEscalationRejection`, `ConfigurationLimitExceededRejection`, `ConfigurationKeyNotFoundRejection`, `TenantLifecycleStateAlreadySetRejection`, `InsufficientPermissionsRejection`, and relevant `NoOp` behavior.

- [x] Add source-backed documentation validation tests. (AC: 1, 2, 3, 4, 5)
  - [x] Add `tests/Hexalith.Tenants.Server.Tests/Documentation/CompensatingCommandsDocumentationTests.cs`.
  - [x] Verify the guide references and matches source-backed contracts: `AddUserToTenant`, `RemoveUserFromTenant`, `ChangeUserRole`, `SetTenantConfiguration`, `RemoveTenantConfiguration`, `DisableTenant`, `EnableTenant`, `TenantRole.TenantOwner`, `TenantRole.TenantContributor`, and `TenantRole.TenantReader`.
  - [x] Verify the guide references source-backed files: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`, `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`, `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`, `src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs`, and `docs/event-contract-reference.md`.
  - [x] Verify JSON command examples parse as valid EventStore command requests and deserialize into current command contracts where examples include payloads.
  - [x] Verify examples use concrete ULID-shaped `messageId` values, `tenant` equals `system`, `domain` equals `tenants`, `aggregateId` matches `payload.TenantId`, and role/status values deserialize by enum name.
  - [x] Verify the guide says correction is not hidden undo, event deletion, event mutation, projection editing, or direct state-store repair.
  - [x] Verify the guide distinguishes successful corrective audit events from rejected command outcomes.
  - [x] Verify unsafe sample content is absent: `Authorization: Bearer `, JWT-like `eyJ...` tokens, `client_secret`, `password=`, full serialized event payload claims, real user data, and stack traces.
  - [x] Verify related docs/navigation link to the guide where adoption readers expect it: `README.md`, `docs/event-contract-reference.md`, `docs/cross-aggregate-timing.md`, and `docs/demo.md`.

- [x] Validate focused tests and record evidence. (AC: 5)
  - [x] Run the focused documentation test class.
  - [x] Run the documentation namespace test suite if the focused class passes.
  - [x] Run focused aggregate tests that anchor compensating-command behavior if available, especially `AddUserToTenant`, `RemoveUserFromTenant`, `ChangeUserRole`, configuration, lifecycle, disabled-tenant, role escalation, and no-op cases.
  - [x] If VSTest hits the known sandbox socket limitation, use the direct xUnit runner fallback pattern recorded in Stories 8.1 through 8.5.
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` only if Epic 8 documentation evidence is still being tracked there.

## Dev Notes

### Source Context

- Epic 8 objective: developers can follow a validated quickstart, understand event contracts, see the reactive access demo, and design for timing, idempotency, and compensating commands. Story 8.6 specifically owns compensating command documentation. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 8: Developers Can Adopt Through Documentation and Demo Evidence`]
- Story 8.6 requires explicit correction workflows, no hidden undo, explicit role specification for `AddUserToTenant`, auditability, common scenarios, rejection cases, and validation against command/role/rejection drift. [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.6: Document Compensating Command Patterns`]
- PRD FR65 requires compensating-command documentation with a worked `AddUserToTenant` after incorrect `RemoveUserFromTenant` example and an explanation that roles are explicitly specified rather than auto-restored. [Source: `_bmad-output/planning-artifacts/prd.md#Documentation & Adoption`]
- PRD Sofia journey defines the product narrative: remove the correct compromised account, re-add the wrongly removed contractor using the role confirmed from event history, preserve the mistake and correction in the audit trail, and avoid implying automatic historical role restoration. [Source: `_bmad-output/planning-artifacts/prd.md#Sofia - Security`]
- Architecture maps Epic 8 documentation/adoption work to `docs/`, `README.md`, and the sample project. It also states events publish through DAPR pub/sub as CloudEvents 1.0 and consumers must assume at-least-once delivery and eventual consistency. [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]

### Current Repository State

- `docs/compensating-commands.md` already exists and contains a wrong-user-removal example. It is currently the right target to audit and harden, not a reason to create a second guide.
- The current guide is thin: it covers only mistaken removal and explicit role restoration. It does not cover wrong role assignment, configuration correction, tenant lifecycle correction, expected rejection cases, command-status/audit distinction, source-backed file references, or validation tests.
- The current guide says compensating commands "undo or correct" a previous operation. For Story 8.6, prefer "correct" or "counteract" and explicitly state that compensation is not hidden undo.
- README currently links quickstart, sample walkthrough, cross-aggregate timing, and demo, but does not link `docs/compensating-commands.md`. Add it to the first-level adoption path and docs tree if still omitted.
- No `CompensatingCommandsDocumentationTests.cs` exists. Existing documentation tests in `tests/Hexalith.Tenants.Server.Tests/Documentation/` are source-backed string/regex/JSON assertions and are the right pattern to extend.

### Technical Guardrails

- Use repo-pinned versions and package families from project context. Do not bump .NET, DAPR, Aspire, xUnit, Shouldly, or package references for this documentation story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- Commands are submitted through EventStore `POST /api/v1/commands`; Tenants does not expose per-command REST endpoints. Command outcome evidence uses `GET /api/v1/commands/status/{correlationId}`. [Source: `docs/quickstart.md`; `Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandsController.cs`; `Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs`]
- The platform tenant in EventStore command requests is `system`; tenant aggregate commands use domain `tenants`, and `aggregateId` should match the managed tenant ID and `payload.TenantId`. [Source: `_bmad-output/project-context.md#Identity Scheme`; `docs/quickstart.md`]
- Public role values are `TenantOwner`, `TenantContributor`, and `TenantReader`; `TenantRole.Unknown` is a non-privileged sentinel and must not appear as a valid corrective role. [Source: `src/Hexalith.Tenants.Contracts/Enums/TenantRole.cs`; `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- `AddUserToTenant` requires a tenant owner or trusted global administrator except for the first member bootstrap path, rejects disabled/missing tenants, rejects duplicate membership with `UserAlreadyInTenantRejection`, and rejects invalid roles with `RoleEscalationRejection`. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`; `docs/event-contract-reference.md`]
- `UserRemovedFromTenant` removes the membership and does not carry role information. The previous role must be recovered from prior `UserAddedToTenant` or `UserRoleChanged` audit/history and chosen explicitly for the correction. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`; `src/Hexalith.Tenants.Contracts/Events/UserRemovedFromTenant.cs`; `src/Hexalith.Tenants.Contracts/Events/UserRoleChanged.cs`]
- `ChangeUserRole` requires a tenant owner or trusted global administrator, rejects disabled/missing tenants, rejects missing users, rejects invalid roles, and returns `NoOp` for same-role changes. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`; `docs/event-contract-reference.md`]
- `SetTenantConfiguration` can correct a wrong value by overwriting the key; same key and same value is `NoOp`. It rejects disabled/missing tenants, unauthorized actors, and key/value/count limit violations. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`; `src/Hexalith.Tenants.Server/Validators/SetTenantConfigurationValidator.cs`]
- `RemoveTenantConfiguration` can correct an accidental key addition when the key should no longer exist. It rejects disabled/missing tenants, unauthorized actors, and absent keys with `ConfigurationKeyNotFoundRejection`. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`; `src/Hexalith.Tenants.Server/Validators/RemoveTenantConfigurationValidator.cs`]
- `DisableTenant` and `EnableTenant` are trusted-global-administrator operations. Repeating the current lifecycle state produces `TenantLifecycleStateAlreadySetRejection`. Most member/configuration commands against disabled tenants reject with `TenantDisabledRejection`, so the guide must be careful about sequencing corrections that require an active tenant. [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`; `docs/event-contract-reference.md`]
- Audit rows are projection-backed. `TenantAuditReadModel` records successful tenant events such as `UserAddedToTenant`, `UserRemovedFromTenant`, `UserRoleChanged`, `TenantConfigurationSet`, `TenantConfigurationRemoved`, `TenantDisabled`, and `TenantEnabled`. It does not make a rejected compensating command look like a successful correction event. [Source: `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`; `src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs`; `docs/event-contract-reference.md`]
- Keep docs support-safe. Do not include raw JWTs, decoded token payloads, secrets, stack traces, real tenant/user data, or full serialized event payload dumps. [Source: `_bmad-output/project-context.md#API Surface`; `_bmad-output/implementation-artifacts/8-5-document-cross-aggregate-timing-and-eventual-consistency.md`]

### Previous Story Intelligence

- Story 8.1 established prerequisite validation, EventStore command/status routes, concrete ULID-shaped command IDs in docs, and the direct xUnit runner fallback when VSTest hits sandbox socket restrictions. Reuse those patterns. [Source: `_bmad-output/implementation-artifacts/8-1-create-a-prerequisite-validated-quickstart.md`; `docs/quickstart.md`]
- Story 8.2 established event contract reference validation, enum serialization checks, source-backed contract tables, and no dependence on persisted English prose in rejection payloads. Link to it instead of duplicating every schema detail. [Source: `_bmad-output/implementation-artifacts/8-2-publish-the-event-contract-reference.md`; `docs/event-contract-reference.md`]
- Story 8.3 established sample walkthrough validation for local projections, support-safe logging, and related-doc navigation. Keep compensating guidance focused on command correction and auditability, not sample projection internals. [Source: `_bmad-output/implementation-artifacts/8-3-document-the-sample-consuming-service-walkthrough.md`; `docs/sample-consuming-service-walkthrough.md`]
- Story 8.4 established demo docs and current command examples for add/remove user flows. If `docs/demo.md` links to compensating guidance, keep it short and avoid changing the demo scope. [Source: `_bmad-output/implementation-artifacts/8-4-produce-the-reactive-access-aha-moment-demo.md`; `docs/demo.md`]
- Story 8.5 established the source-backed documentation-test style for timing claims, README navigation updates, no fixed-delay correctness, and no live infrastructure proof without Docker/Aspire evidence. Use the same evidence discipline. [Source: `_bmad-output/implementation-artifacts/8-5-document-cross-aggregate-timing-and-eventual-consistency.md`; `tests/Hexalith.Tenants.Server.Tests/Documentation/CrossAggregateTimingDocumentationTests.cs`]
- Recent commits show Stories 8.1 through 8.5 landed immediately before this story, so quickstart, event contract reference, sample walkthrough, demo, and timing docs are current sources to link rather than rewrite. [Source: `git log --oneline -5`]
- Dirty worktree note at story creation: `_bmad-output/story-automator/orchestration-7-20260601-143204.md` already had unrelated local modifications. Do not restore or rewrite that file during Story 8.6 implementation.

### Latest Technical Notes

- Current Microsoft compensating-transaction guidance frames compensation as application-specific work that counteracts completed steps rather than an automatic rollback. That supports Tenants' explicit command guidance. [Source: Microsoft Learn, Compensating Transaction pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/compensating-transaction)
- Current Microsoft event-sourcing guidance emphasizes append-only event storage as an audit trail and notes that bad historical data may require compensating events or upcasters rather than mutating history. [Source: Microsoft Learn, Event Sourcing pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing)
- Current DAPR pub/sub guidance still states at-least-once delivery. Compensating-command docs must not treat subscriber delivery as synchronous proof of correction; link to `docs/cross-aggregate-timing.md` for timing and projection catch-up. [Source: DAPR Docs, Publish and subscribe overview](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/)

### Existing Files Likely to Touch

- `docs/compensating-commands.md`: primary documentation target.
- `README.md`: add first-level adoption and docs-tree link if omitted.
- `docs/event-contract-reference.md`: add a concise related-doc link to compensating commands if omitted.
- `docs/cross-aggregate-timing.md`: add a concise related-doc link to compensating commands if omitted.
- `docs/demo.md`: add a concise related-doc link to compensating commands if useful for the wrong-user/restoration story.
- `tests/Hexalith.Tenants.Server.Tests/Documentation/CompensatingCommandsDocumentationTests.cs`: new source-backed documentation validation.
- `tests/Hexalith.Tenants.Server.Tests/Documentation/EventContractReferenceDocumentationTests.cs` or related documentation tests: update only if navigation assertions need to include the compensating guide.
- `_bmad-output/implementation-artifacts/tests/test-summary.md`: update only if Epic 8 documentation validation evidence continues being recorded there.

### Project Structure Notes

- Alignment: Story 8.6 belongs in `docs/`, README navigation, and existing documentation tests. It should not change domain behavior, command contracts, projection semantics, DAPR component names, package versions, or production deployment posture unless validation exposes concrete source drift.
- Boundary: do not implement rollback, event deletion, event mutation, projection edits, direct DAPR state repair, or an automatic role-restore feature.
- Boundary: do not add new command contracts for compensation. Use existing explicit commands and document their current rejection/no-op behavior.
- Boundary: do not implement the planned synchronous authorization plugin. Reference it only through timing docs if needed.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.6: Document Compensating Command Patterns`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Documentation & Adoption`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Sofia - Security`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]
- [Source: `_bmad-output/project-context.md#Identity Scheme`]
- [Source: `_bmad-output/project-context.md#API Surface`]
- [Source: `docs/compensating-commands.md`]
- [Source: `docs/event-contract-reference.md`]
- [Source: `docs/cross-aggregate-timing.md`]
- [Source: `docs/quickstart.md`]
- [Source: `docs/demo.md`]
- [Source: `README.md`]
- [Source: `Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandsController.cs`]
- [Source: `Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs`]
- [Source: `src/Hexalith.Tenants.Contracts/Commands/AddUserToTenant.cs`]
- [Source: `src/Hexalith.Tenants.Contracts/Commands/RemoveUserFromTenant.cs`]
- [Source: `src/Hexalith.Tenants.Contracts/Commands/ChangeUserRole.cs`]
- [Source: `src/Hexalith.Tenants.Contracts/Commands/SetTenantConfiguration.cs`]
- [Source: `src/Hexalith.Tenants.Contracts/Commands/RemoveTenantConfiguration.cs`]
- [Source: `src/Hexalith.Tenants.Contracts/Commands/DisableTenant.cs`]
- [Source: `src/Hexalith.Tenants.Contracts/Commands/EnableTenant.cs`]
- [Source: `src/Hexalith.Tenants.Contracts/Enums/TenantRole.cs`]
- [Source: `src/Hexalith.Tenants.Contracts/Enums/TenantStatus.cs`]
- [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantState.cs`]
- [Source: `src/Hexalith.Tenants.Server/Projections/TenantAuditReadModel.cs`]
- [Source: `src/Hexalith.Tenants.Contracts/Queries/TenantAuditEntry.cs`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Documentation/CrossAggregateTimingDocumentationTests.cs`]
- [Source: Microsoft Learn, Compensating Transaction pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/compensating-transaction)
- [Source: Microsoft Learn, Event Sourcing pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/event-sourcing)
- [Source: DAPR Docs, Publish and subscribe overview](https://docs.dapr.io/developing-applications/building-blocks/pubsub/pubsub-overview/)

## Validation Checklist Results

- Story foundation: PASS. Story statement and all five Epic 8.6 acceptance criteria are preserved.
- Scope control: PASS. The story limits implementation to compensating-command documentation, related navigation, source-backed documentation tests, and evidence recording.
- Architecture/source context: PASS. The story cites EventStore command routes, Tenants command contracts, aggregate behavior, audit projection behavior, Epic/PRD/architecture source documents, and current related docs.
- Reinvention prevention: PASS. The story directs the developer to audit and harden existing `docs/compensating-commands.md` instead of creating a parallel guide or new command abstractions.
- Wrong-library/version prevention: PASS. The story keeps repo-pinned .NET/DAPR/Aspire/testing versions and uses external docs only to confirm current pattern semantics.
- File-location prevention: PASS. Expected changes are limited to `docs/`, README/navigation, existing documentation tests, and optional validation evidence.
- Regression prevention: PASS. The story calls out command route contracts, aggregate rejection/no-op behavior, audit projection boundaries, support-safe examples, and timing-doc links.
- Security/privacy prevention: PASS. The story forbids raw tokens, decoded JWT payloads, secrets, stack traces, real tenant/user data, and full serialized event payload dumps.
- Validation evidence: PASS. The story requires source-backed documentation tests plus focused aggregate behavior validation where useful.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- VSTest focused run built successfully, then aborted before execution with sandbox `SocketException (13): Permission denied`; direct xUnit runner fallback was used.
- Focused documentation tests: `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Documentation.CompensatingCommandsDocumentationTests -parallel none -noLogo -noColor` — passed 9 total, 0 failed, 0 skipped.
- Documentation namespace regression: `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -namespace Hexalith.Tenants.Server.Tests.Documentation -parallel none -noLogo -noColor` — passed 42 total, 0 failed, 0 skipped.
- Aggregate behavior regression: `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Aggregates.TenantAggregateTests -parallel none -noLogo -noColor` — passed 140 total, 0 failed, 0 skipped.
- Full direct xUnit regression suite across Contracts, Client, Testing, Server, Sample, and Integration assemblies — passed 1349 total, 0 failed, 27 prerequisite-gated skips.
- Senior developer review validation passed: Server.Tests build 0 warnings/errors; focused documentation tests 9/9; documentation namespace 42/42; aggregate behavior tests 140/140; full direct xUnit suite 1349 total, 0 failed, 27 skipped.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Rewrote `docs/compensating-commands.md` around explicit follow-up commands through `POST /api/v1/commands`, immutable event history, command-status evidence, projection-backed audit evidence, and rejection/no-op behavior.
- Documented source-backed correction paths for mistaken user removal, wrong role assignment, configuration mistakes, and tenant lifecycle mistakes without hidden undo, event mutation, projection edits, or direct state-store repair.
- Added navigation from README, event contract reference, timing guide, and demo guide so adoption readers can find compensating-command guidance.
- Added source-backed documentation tests that validate command examples, enum deserialization, source references, audit/rejection distinctions, support-safe sample constraints, and related-doc navigation.
- Recorded Epic 8 documentation validation evidence in `_bmad-output/implementation-artifacts/tests/test-summary.md`.
- Senior developer review completed. Stale validation counts in this story record were corrected, and no source-code or documentation defects remain.

### File List

- README.md
- _bmad-output/implementation-artifacts/8-6-document-compensating-command-patterns.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- _bmad-output/implementation-artifacts/tests/test-summary.md
- docs/compensating-commands.md
- docs/cross-aggregate-timing.md
- docs/demo.md
- docs/event-contract-reference.md
- tests/Hexalith.Tenants.Server.Tests/Documentation/CompensatingCommandsDocumentationTests.cs

## Senior Developer Review (AI)

Reviewer: GPT-5 Codex  
Date: 2026-06-01  
Outcome: Approve

### Review Findings

- [x] [AI-Review][Low] The story Dev Agent Record had stale validation counts from before the QA documentation-test additions: focused documentation tests were recorded as 7 instead of 9, documentation namespace as 40 instead of 42, and full direct suite as 1347 instead of 1349. Corrected the story record to match the current test summary and rerun evidence.

### Acceptance Criteria Validation

- AC1: PASS. The guide explains that mistaken user removal is corrected with a new explicit command such as `AddUserToTenant` with an explicit role, and it rejects hidden undo language.
- AC2: PASS. The worked example states why the intended role is explicit and does not imply automatic historical role restoration.
- AC3: PASS. The guide preserves immutable original events and separates command-status evidence from projection-backed audit rows.
- AC4: PASS. The guide covers mistaken removal, wrong role assignment, configuration mistakes, and lifecycle corrections with safe command paths plus rejection/no-op cases.
- AC5: PASS. Source-backed documentation tests bind command names, roles, routes, rejection behavior, JSON examples, safety exclusions, and related navigation to current contracts and source files.

### Git and File List Validation

- Story File List covers the story-related source and documentation changes: README, compensating-command guide, related docs, documentation tests, story file, sprint status, and test summary.
- `_bmad-output/story-automator/orchestration-7-20260601-143204.md` is an orchestration artifact outside application source review scope. It remains unlisted in the story File List and was not modified during this review.

### Documentation References Checked

- Microsoft Learn compensating-transaction guidance still frames compensation as application-specific follow-up work rather than simple rollback.
- Microsoft Learn event-sourcing guidance still describes append-only event storage and compensating events rather than mutating historical events.
- DAPR docs still state pub/sub uses at-least-once delivery, supporting the guide's separation between command outcome and subscriber/projection catch-up.

### Review Validation

- `dotnet build tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore -m:1 /nr:false /p:BuildInParallel=false` - PASS, 0 warnings, 0 errors.
- `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Documentation.CompensatingCommandsDocumentationTests -parallel none -noLogo -noColor` - PASS, 9 total, 0 failed, 0 skipped.
- `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -namespace Hexalith.Tenants.Server.Tests.Documentation -parallel none -noLogo -noColor` - PASS, 42 total, 0 failed, 0 skipped.
- `dotnet tests/Hexalith.Tenants.Server.Tests/bin/Debug/net10.0/Hexalith.Tenants.Server.Tests.dll -class Hexalith.Tenants.Server.Tests.Aggregates.TenantAggregateTests -parallel none -noLogo -noColor` - PASS, 140 total, 0 failed, 0 skipped.
- Full direct xUnit suite across Contracts, Client, Testing, Server, Sample, and Integration assemblies - PASS, 1349 total, 0 failed, 27 prerequisite-gated skips.

### Change Log

- 2026-06-01: Rewrote compensating-command guide with explicit correction workflows, current rejection/no-op cases, auditability guidance, and support-safe examples.
- 2026-06-01: Added source-backed documentation validation and navigation links for the compensating-command guide.
- 2026-06-01: Validated focused documentation tests, documentation namespace regression, aggregate behavior tests, and full direct xUnit regression suite.
- 2026-06-01: Senior review corrected stale validation counts, verified acceptance criteria and current source-backed behavior, and marked story done.
