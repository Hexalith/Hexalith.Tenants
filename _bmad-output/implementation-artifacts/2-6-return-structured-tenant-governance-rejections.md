---
baseline_commit: bd1e935a89d64a4aba146c544722a877056c85c4
---

# Story 2.6: Return Structured Tenant Governance Rejections

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a tenant API consumer,
I want tenant governance failures to return structured, actionable error responses,
so that I can correct invalid commands without inspecting logs or persisted event payload prose.

## Acceptance Criteria

1. Given a tenant governance command fails a business rule, when the aggregate returns a rejection, then the rejection event payload contains structured data only, and it does not contain localized or user-facing English prose.
2. Given a command targets a missing tenant, when the API maps the rejection to an HTTP response, then the response uses RFC 7807 Problem Details, and the status code and `type` identify the tenant-not-found rejection.
3. Given a command targets a disabled tenant or duplicate state, when the API maps the rejection to an HTTP response, then the response uses the configured rejection-to-status mapping, and the response includes a safe corrective action hint composed at the HTTP boundary.
4. Given a command fails authorization or escalation checks, when Problem Details are returned, then the response does not leak command payloads, event payloads, tokens, stack traces, or sensitive tenant/user data.
5. Given rejection mapping tests run, when all tenant governance rejections are exercised, then each rejection has a deterministic HTTP mapping, and unmapped rejections fail tests or use an explicitly documented default.

## Tasks / Subtasks

- [x] Preserve structured rejection contracts and remove prose from persisted payloads (AC: 1, 4)
  - [x] Audit every `src/Hexalith.Tenants.Contracts/Events/Rejections/*.cs` record and confirm fields are structured identifiers/enums/counts only.
  - [x] Do not add `Message`, `Reason`, `Detail`, localized text, exception text, payload JSON, token values, or stack traces to any `IRejectionEvent`.
  - [x] Keep rejection events as immutable records implementing `IRejectionEvent`; do not replace domain rejections with thrown exceptions in aggregate `Handle` methods.
  - [x] Keep existing replay-only rejection `Apply` methods in aggregate state non-mutating.

- [x] Harden the HTTP Problem Details boundary for tenant governance rejections (AC: 2, 3, 4, 5)
  - [x] Reuse EventStore's `DomainCommandRejectedExceptionHandler` and `DomainRejectionProblemCatalog`; do not create per-command controllers or a second rejection-mapping pipeline in Tenants.
  - [x] Ensure `POST /api/v1/commands` returns `application/problem+json` for domain rejections raised through `DomainCommandRejectedException`.
  - [x] Verify Problem Details includes safe machine-readable fields: `type`, `title`, `status`, `instance`, `correlationId`, `tenantId`, `reasonCode`, `rejectionType`, and `correctiveAction`.
  - [x] For missing-resource rejections (`TenantNotFoundRejection`, `GlobalAdministratorNotFoundRejection`), assert HTTP 404 and domain-rejection type URIs/reason codes identifying the rejection.
  - [x] For duplicate/current-state rejections whose reason code contains `already` or `duplicate` (`TenantAlreadyExistsRejection`, `TenantLifecycleStateAlreadySetRejection`, `UserAlreadyInTenantRejection`, `GlobalAdminAlreadyBootstrappedRejection`, `GlobalAdministratorAlreadyExistsRejection`), assert HTTP 409.
  - [x] For disabled, authorization, escalation, configuration-limit, user-not-in-tenant, and last-admin rejections, assert HTTP 422 unless EventStore's catalog is intentionally changed with tests and docs.
  - [x] Confirm the `correctiveAction` extension is composed by the HTTP boundary/catalog and is not copied from persisted event payload data.

- [x] Add deterministic coverage for every Tenants rejection type (AC: 2, 3, 4, 5)
  - [x] Add or extend a focused integration test fixture around `CommandApiRuntimeIntegrationTests` that enumerates all current Tenants rejection event types and expected status codes.
  - [x] Fail if a rejection type exists in `Hexalith.Tenants.Contracts.Events.Rejections` without an explicit expected HTTP status in the Tenants test suite.
  - [x] Include at least these rejections: `TenantNotFoundRejection`, `TenantDisabledRejection`, `TenantAlreadyExistsRejection`, `TenantLifecycleStateAlreadySetRejection`, `InsufficientPermissionsRejection`, `RoleEscalationRejection`, `ConfigurationLimitExceededRejection`, `UserAlreadyInTenantRejection`, `UserNotInTenantRejection`, `GlobalAdminAlreadyBootstrappedRejection`, `GlobalAdministratorAlreadyExistsRejection`, `GlobalAdministratorNotFoundRejection`, and `LastGlobalAdministratorRejection`.
  - [x] Verify the Problem Details response does not include inbound command payload JSON, serialized rejection event payload JSON, bearer tokens, stack traces, or sensitive synthetic tenant/user values outside the documented safe extension fields.
  - [x] Keep tests xUnit v3 + Shouldly; do not use `Assert.*`.

- [x] Preserve command and aggregate behavior while improving response mapping (AC: 1-5)
  - [x] Do not change successful create/update/disable/enable/global-admin/member/configuration event semantics for this story.
  - [x] Preserve canonical tenant identity behavior: lifecycle governance commands use `envelope.AggregateId`; member/configuration command contracts continue to carry and validate their tenant ID according to existing aggregate logic.
  - [x] Preserve ordering in `TenantAggregate`: missing tenant before disabled tenant for tenant-scoped commands, disabled/unknown status before RBAC for non-enable mutations, and trusted `actor:globalAdmin` extension only for global-admin authority.
  - [x] Do not convert current no-op cases into rejections unless an acceptance criterion or existing story explicitly requires it; this story is about the HTTP mapping of actual rejections.

- [x] Update public documentation to match the implemented Problem Details shape (AC: 2, 3, 5)
  - [x] Update `docs/event-contract-reference.md#RFC 7807 Problem Details`; it currently describes the older shape where `type` is just the rejection event name and `title` is derived from HTTP status.
  - [x] Document the actual domain-rejection URI form, `reasonCode`, fully qualified `rejectionType`, and `correctiveAction` extension.
  - [x] Keep documentation support-safe: examples must not expose raw command payloads, tokens, stack traces, local paths, or sensitive user data.
  - [x] Align the rejection table with the deterministic status-code expectations used by tests.

- [x] Run focused validation (AC: 1-5)
  - [x] Run contracts validation:
    `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Contracts.Tests --filter "EventSerializationTests|NamingConventionTests" -m:1 -nr:false`
  - [x] Run command API Problem Details validation:
    `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.IntegrationTests --filter "CommandApiRuntimeIntegrationTests" -m:1 -nr:false`
  - [x] Run focused aggregate validation if aggregate ordering is touched:
    `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Server.Tests --filter "TenantAggregateTests|CommandPipelineIntegrationTests" -m:1 -nr:false`
  - [x] Run release build:
    `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false`
  - [x] If local VSTest socket restrictions recur, record the exact `System.Net.Sockets.SocketException (13): Permission denied` diagnostics and use successful build/test-project compilation as partial evidence only.

## Dev Notes

### Source Context

- Epic 2 objective: global administrators can bootstrap, manage administrators, create/update tenants, disable/enable tenants, and receive structured rejection outcomes. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 2: Global Administrators Can Bootstrap and Govern Tenants`]
- Story 2.6 explicitly requires structured rejection payloads, RFC 7807 Problem Details for API mapping, safe corrective action hints at the HTTP boundary, no sensitive leakage, and deterministic coverage for all tenant governance rejections. [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.6: Return Structured Tenant Governance Rejections`]
- PRD FR49-FR52 require actionable command rejections, missing-tenant rejection, disabled-tenant rejection, and duplicate/current-state context. [Source: `_bmad-output/planning-artifacts/prd.md#Functional Requirements`]
- Architecture requires domain rejection responses to use Problem Details, rejection `type`/reason metadata to identify the rejection, rejection payloads to contain structured data only, and user-facing text to be composed outside persisted payloads. [Source: `_bmad-output/planning-artifacts/architecture.md#Format Patterns`]
- UX planning depends on safe rejection messaging for future command-capable flows: accepted submission is not confirmed outcome, rejected commands must explain safe next actions, and UI text must not expose payloads, tokens, stack traces, internal correlation IDs, or sensitive tenant/user data. [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Key Design Challenges`]

### Current Repository State

- `src/Hexalith.Tenants/Program.cs` already registers `AddProblemDetails()` and `DomainCommandRejectedExceptionHandler`, then imports EventStore's `CommandsController` through `AddApplicationPart`.
- The command gateway is EventStore's `POST /api/v1/commands`, not a Tenants-owned per-command controller.
- EventStore's `SubmitCommandHandler` throws `DomainCommandRejectedException` when command processing returns `Accepted=false` and command status contains `RejectionEventType`.
- EventStore's `DomainCommandRejectedExceptionHandler` builds RFC 7807 `ProblemDetails` with `correlationId`, `tenantId`, `reasonCode`, `rejectionType`, and `correctiveAction` extensions, using `DomainRejectionProblemCatalog`.
- EventStore's `DomainRejectionProblemCatalog` currently maps reason codes containing `not-found` to 404, containing `already` or `duplicate` to 409, and all other domain rejections to 422. This means `GlobalAdminAlreadyBootstrappedRejection` and `GlobalAdministratorAlreadyExistsRejection` are 409 by current catalog behavior, while `GlobalAdministratorNotFoundRejection` is 404.
- Existing integration tests already cover selected mappings for `GlobalAdminAlreadyBootstrappedRejection`, `TenantAlreadyExistsRejection`, `TenantLifecycleStateAlreadySetRejection`, `TenantNotFoundRejection`, and `TenantDisabledRejection`; this story should close the gap for every Tenants rejection type and leakage assertions.
- Current Tenants rejection records are structured and short. None currently carry prose fields, but this should be enforced by tests/review guidance rather than assumed.
- `docs/event-contract-reference.md#RFC 7807 Problem Details` is stale: it says `type` is the rejection event name and `title` is the HTTP status text, while current EventStore code uses a domain-rejection type URI and title derived from the rejection reason code.

### Architecture and Technical Guardrails

- Runtime stack remains .NET 10 SDK `10.0.300`, C# latest, nullable enabled, warnings as errors, and central package management. Do not bump SDK or packages for this story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- Use `Hexalith.Tenants.slnx`; do not create or use a legacy `.sln`.
- Use `System.Text.Json` only. Do not introduce Newtonsoft.Json or another serializer.
- Contracts are immutable records in `Hexalith.Tenants.Contracts`; rejection events live under `src/Hexalith.Tenants.Contracts/Events/Rejections/` and implement `IRejectionEvent`.
- Aggregate `Handle` methods must remain pure static functions: no I/O, no DAPR, no async, no logging, and no captured state.
- Business-rule failures return `DomainResult.Rejection([new XxxRejection(...)])`; do not throw from aggregate `Handle` methods for domain failures.
- Rejection events are persisted and may be published; treat their payload shape as a public contract.
- Problem Details mapping belongs at the HTTP boundary. Persisted rejections carry structured facts; `correctiveAction` and other user-facing text are response-layer/catalog concerns.
- Preserve safe logging rules: domain rejections are normal business outcomes, not infrastructure errors; logs and Problem Details must not include command payloads, tokens, secrets, stack traces, or sensitive user data.
- Use K&R brace style, file-scoped namespaces, one type per file, folder structure matching namespaces, and no inline `PackageReference Version=`.
- No external package or API research is needed: this story uses existing pinned .NET/EventStore/Tenants APIs and introduces no dependency upgrades.

### Previous Story Intelligence

- Story 2.5 replaced duplicate lifecycle no-ops with `TenantLifecycleStateAlreadySetRejection`, added fail-closed `TenantStatus.Unknown` handling, and added command API coverage for duplicate lifecycle and disabled-tenant rejections.
- Story 2.5 confirmed local focused test commands may build successfully and then abort under VSTest with `System.Net.Sockets.SocketException (13): Permission denied`; record that exact blocker if it recurs.
- Story 2.4 and 2.5 both reinforced canonical `envelope.AggregateId` usage for tenant lifecycle commands. Do not regress to trusting command-body tenant IDs for lifecycle governance.
- Story 2.3 introduced trusted `actor:globalAdmin` envelope handling and client-supplied reserved extension sanitization. Do not weaken this while adding rejection response tests.
- Recent commits:
  - `bd1e935 feat(story-2.5): disable and re-enable tenants`
  - `c996c3b feat(story-2.4): create and update tenants`
  - `1c58824 feat(story-2.3): authorize global administrators for cross-tenant governance`
  - `9240d0c feat(tests): add global admin extension handling in integration tests and telemetry`
  - `8a0b2a1 change BMAD project name from Hexalith.Tenants to Tenants`

### Likely Files to Touch

- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/EventSerializationTests.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/NamingConventionTests.cs`
- `docs/event-contract-reference.md`
- `src/Hexalith.Tenants/Program.cs` only if registration/order is proven wrong by tests
- `Hexalith.EventStore/src/Hexalith.EventStore/ErrorHandling/DomainRejectionProblemCatalog.cs` only if a deterministic status/default gap must be fixed at the shared EventStore boundary
- `Hexalith.EventStore/src/Hexalith.EventStore/ErrorHandling/DomainCommandRejectedExceptionHandler.cs` only if safe corrective-action/leakage behavior is proven insufficient

### Out of Scope

- New tenant governance commands or new rejection records unless a missing mapping test exposes an actual domain gap.
- Changing successful tenant lifecycle, global administrator, membership, or configuration event semantics.
- Replacing EventStore's command gateway, creating per-command Tenants REST endpoints, or changing `/process` domain processor behavior.
- Phase 2 UI implementation, FrontComposer, localization implementation, or visual command lifecycle surfaces.
- Query endpoint Problem Details mapping, cursor pagination, projection write conflict recovery, or audit query behavior from Epic 5.
- SDK, package, DAPR, Aspire, OpenTelemetry, or submodule topology upgrades.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 2.6: Return Structured Tenant Governance Rejections`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Command Validation & Error Handling`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Error Handling Patterns`]
- [Source: `_bmad-output/planning-artifacts/ux-design-specification.md#Key Design Challenges`]
- [Source: `_bmad-output/project-context.md#API Surface`]
- [Source: `_bmad-output/project-context.md#Handle Methods (Aggregates) - Hard Rules`]
- [Source: `_bmad-output/implementation-artifacts/2-5-disable-and-re-enable-tenants.md`]
- [Source: `src/Hexalith.Tenants/Program.cs`]
- [Source: `Hexalith.EventStore/src/Hexalith.EventStore/ErrorHandling/DomainCommandRejectedExceptionHandler.cs`]
- [Source: `Hexalith.EventStore/src/Hexalith.EventStore/ErrorHandling/DomainRejectionProblemCatalog.cs`]
- [Source: `Hexalith.EventStore/src/Hexalith.EventStore.Server/Pipeline/SubmitCommandHandler.cs`]
- [Source: `src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs`]
- [Source: `docs/event-contract-reference.md#RFC 7807 Problem Details`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`]

## Project Structure Notes

- Alignment: this story belongs mostly in integration tests and public docs, with possible shared EventStore boundary changes only if the current catalog/handler cannot satisfy deterministic mapping and leakage requirements.
- The main implementation risk is duplicating EventStore's existing Problem Details mapping inside Tenants. Prefer tests that lock down the imported EventStore behavior, then make the smallest boundary fix if the shared behavior is insufficient.
- The second implementation risk is stale documentation. Update examples to match the current URI/reason-code/corrective-action shape so consumers do not build against the older `type: "TenantNotFoundRejection"` contract.

## Validation Checklist Results

- Story foundation extracted from Epic 2 and Story 2.6 acceptance criteria.
- PRD, architecture, UX, project context, previous Story 2.5, current Tenants source, imported EventStore command/Problem Details source, docs, tests, and recent git history were reviewed.
- Current source inspection found that most HTTP mapping infrastructure already exists in EventStore; the story should focus on deterministic all-rejection coverage, safe leakage checks, and docs correction.
- Existing UPDATE files and likely affected areas were identified, including `CommandApiRuntimeIntegrationTests`, event-contract docs, and EventStore mapping code if tests expose a shared boundary gap.
- Disaster-prevention guardrails included: do not duplicate the command gateway, do not add prose to persisted rejections, do not expose payloads/tokens/stack traces, do not weaken trusted global-admin extension handling, and do not change aggregate success semantics.
- Latest technical context checked against local pinned project rules. No external dependency research is required because no package or framework update is part of this story.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-31: `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Contracts.Tests --filter "EventSerializationTests|NamingConventionTests" -m:1 -nr:false` compiled the test project, then VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied` while starting `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer`.
- 2026-05-31: `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.IntegrationTests --filter "CommandApiRuntimeIntegrationTests" -m:1 -nr:false` compiled the test project, then VSTest aborted with `System.Net.Sockets.SocketException (13): Permission denied` while starting `Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.SocketServer`.
- 2026-05-31: `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false` passed with 0 warnings and 0 errors.
- 2026-05-31: `dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Release/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class Hexalith.Tenants.Contracts.Tests.EventSerializationTests -class Hexalith.Tenants.Contracts.Tests.NamingConventionTests -noLogo -noColor` passed: 29 total, 0 failed.
- 2026-05-31: `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Release/net10.0/Hexalith.Tenants.IntegrationTests.dll -class Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests -noLogo -noColor` passed: 48 total, 0 failed.

### Completion Notes List

- Added reflection guardrails so Tenants rejection event contracts reject prose/sensitive payload fields such as `Message`, `Reason`, `Detail`, `Payload`, `Token`, and stack trace fields while allowing structured identifiers, enums, and counts.
- Added deterministic `CommandApiRuntimeIntegrationTests` coverage for every current Tenants rejection type, including explicit 404/409/422 expectations, all required Problem Details fields, `application/problem+json`, safe corrective action, and leakage markers.
- Hardened EventStore's domain rejection HTTP boundary so client-facing `detail` is composed from `DomainRejectionProblemCatalog` instead of echoing raw `DomainCommandRejectedException` detail text.
- Updated the reserved global-admin extension metadata integration test to reflect the current gateway behavior: client-supplied `actor:globalAdmin` is rejected as invalid extension metadata before routing.
- Updated the public event contract reference to document the actual domain-rejection URI, `reasonCode`, fully qualified `rejectionType`, `correctiveAction`, and deterministic rejection status table.
- Aggregate command/event semantics and `TenantAggregate` ordering were not changed; focused aggregate validation was not applicable beyond the successful release build because no aggregate code was touched.

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-05-31

Outcome: Approved after automatic fixes.

Findings fixed:

- MEDIUM: `baseline_commit` used an invalid full SHA (`bd1e935179fc1e78e2fbac2f74901fbc349b84da`), which prevented deterministic review comparison from the story metadata. Updated it to the actual Story 2.5 baseline commit `bd1e935a89d64a4aba146c544722a877056c85c4`.
- MEDIUM: `DomainCommandRejectedExceptionHandler` still logged normal domain rejections at `Warning`, conflicting with this story's guardrail that domain rejections are normal business outcomes rather than infrastructure errors. Changed the log level to `Information` while preserving safe structured fields only.
- MEDIUM: EventStore's nearby command lifecycle integration expectation still described the older Problem Details `type` shape as the rejection event name. Updated it to assert the stable domain-rejection reason-code URI suffix.

Validation performed:

- `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -warnaserror -m:1 -nr:false` - passed with 0 warnings and 0 errors.
- `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.Contracts.Tests --configuration Release --no-build --filter "EventSerializationTests|NamingConventionTests" -m:1 -nr:false` - VSTest aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied`.
- `dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Release/net10.0/Hexalith.Tenants.Contracts.Tests.dll -class Hexalith.Tenants.Contracts.Tests.EventSerializationTests -class Hexalith.Tenants.Contracts.Tests.NamingConventionTests -noLogo -noColor` - passed: 29 total, 0 failed.
- `MSBUILDDISABLENODEREUSE=1 dotnet test tests/Hexalith.Tenants.IntegrationTests --configuration Release --no-build --filter "CommandApiRuntimeIntegrationTests" -m:1 -nr:false` - VSTest aborted before execution with `System.Net.Sockets.SocketException (13): Permission denied`.
- `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Release/net10.0/Hexalith.Tenants.IntegrationTests.dll -class Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests -noLogo -noColor` - passed: 48 total, 0 failed.
- `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.EventStore/tests/Hexalith.EventStore.IntegrationTests/Hexalith.EventStore.IntegrationTests.csproj --configuration Release -warnaserror -m:1 -nr:false` - blocked by restricted network access to `https://api.nuget.org/v3/index.json` during restore.

### File List

- `_bmad-output/implementation-artifacts/2-6-return-structured-tenant-governance-rejections.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/story-automator/orchestration-1-20260531-113112.md`
- `Hexalith.EventStore/src/Hexalith.EventStore/ErrorHandling/DomainCommandRejectedExceptionHandler.cs`
- `Hexalith.EventStore/tests/Hexalith.EventStore.IntegrationTests/ContractTests/CommandLifecycleTests.cs`
- `docs/event-contract-reference.md`
- `tests/Hexalith.Tenants.Contracts.Tests/NamingConventionTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/CommandApiRuntimeIntegrationTests.cs`

### Change Log

- 2026-05-31: Implemented structured rejection contract guardrails, deterministic Problem Details mapping coverage for all Tenants rejections, safe HTTP-domain rejection detail composition, and updated public event contract documentation.
- 2026-05-31: Senior review fixed story baseline metadata, lowered normal domain rejection logging to Information, and aligned EventStore integration-test expectations with the domain-rejection URI Problem Details shape.
