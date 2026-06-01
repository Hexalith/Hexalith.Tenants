---
baseline_commit: 11a32f81d94fc3cf81a729de22b658cc2e3c2ed3
---

# Story 8.1: Create a Prerequisite-Validated Quickstart

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer evaluating Tenants,
I want a quickstart that validates prerequisites before the first command,
so that I can reach my first tenant command within 30 minutes without guessing at environment setup.

## Acceptance Criteria

1. Given a developer opens the quickstart, when they begin setup, then the guide lists required .NET SDK, root-level submodule initialization, DAPR, EventStore, and local runtime prerequisites, and it explicitly avoids recursive submodule initialization.
2. Given the developer follows prerequisite validation, when DAPR, EventStore, AppHost, or authentication prerequisites are missing, then the guide explains how to detect and fix the missing prerequisite, and failures are identified before the first tenant command is submitted.
3. Given prerequisites are satisfied, when the developer follows the quickstart path, then they can restore, build, start the required local topology, and submit a first tenant command within the target 30-minute journey, and the command path uses the documented EventStore command submission route.
4. Given the first command succeeds or rejects, when the developer inspects the outcome, then the guide explains how to identify success, structured rejection, and next corrective action, and it does not require reading raw logs as the primary success signal.
5. Given quickstart validation is tested, when a reviewer follows the guide on a prepared environment, then commands, paths, package names, and expected outputs are current, and any local-only assumptions are clearly labeled.

## Tasks / Subtasks

- [x] Audit and update `docs/quickstart.md` prerequisite validation. (AC: 1, 2)
  - [x] Keep the 30-minute target scoped to the clone-to-first-command journey with prerequisites already installed.
  - [x] List and validate required prerequisites before command submission: .NET 10 SDK, Docker, full DAPR local runtime, root-level submodules, Aspire AppHost startup, EventStore command gateway availability, and local authentication token acquisition.
  - [x] Preserve the exact root-level submodule command and explicitly warn not to use recursive submodule initialization.
  - [x] Keep DAPR local guidance aligned with full `dapr init`; explain that `--slim` excludes services the actor/AppHost path needs unless the operator supplies them separately.
  - [x] Add detection/remediation steps that let a developer identify failures before the first command: missing SDK, uninitialized submodules, Docker unavailable, DAPR runtime not initialized, AppHost resource unavailable, Keycloak/token failure, or wrong/missing tenant/auth claim.

- [x] Verify and document the first-command path against source of truth. (AC: 3, 4)
  - [x] Confirm the quickstart uses the EventStore command gateway route `POST /api/v1/commands`; do not introduce a Tenants-specific per-command controller path.
  - [x] Confirm the guide starts the local topology through `src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj` and explains that EventStore, Tenants, Keycloak, Redis, DAPR sidecars, and the sample service are AppHost-managed in local mode.
  - [x] Keep the required command sequence explicit: `BootstrapGlobalAdmin` against domain `global-administrators`, then `CreateTenant` against domain `tenants`.
  - [x] Keep `messageId` examples ULID-shaped and unique per command; do not show literal placeholder values that the controller would reject.
  - [x] Keep `CreateTenant` examples aligned with the contract: `aggregateId` and `payload.TenantId` must match the managed tenant ID.
  - [x] Explain success through `202 Accepted`, the returned correlation ID, command-status polling, `Completed` with produced events, and the tenant query endpoint.
  - [x] Explain structured rejection through command status, `rejectionEventType`, and corrective action for known quickstart repeats such as `GlobalAdminAlreadyBootstrappedRejection` and `TenantAlreadyExistsRejection`.

- [x] Tighten README and cross-document navigation only where it prevents quickstart confusion. (AC: 1, 5)
  - [x] Update `README.md` only if the current quickstart time estimate, prerequisites, package list, or quickstart link drift from the final guide.
  - [x] Keep production auth details in `docs/production-auth-claim-contract.md` and `docs/production-auth-readiness.md`; the quickstart may link to them but must stay local-development focused.
  - [x] Link deployment-specific DAPR details to `deploy/dapr/README.md` instead of duplicating production/slim-mode guidance in the core quickstart.

- [x] Add validation evidence for the quickstart content. (AC: 5)
  - [x] Verify referenced files and commands exist: `Hexalith.Tenants.slnx`, AppHost project path, `docs/production-auth-claim-contract.md`, `docs/production-auth-readiness.md`, `deploy/dapr/README.md`, sample project, and scripts/docs links already in the README.
  - [x] Verify source routes and config before editing examples: EventStore command route usage in tests/source, `src/Hexalith.Tenants.AppHost/Program.cs` Keycloak/AppHost wiring, `src/Hexalith.Tenants/appsettings.json` domain service registrations, and query routes in `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`.
  - [x] Run a markdown/link/static check if the repository has one; otherwise run focused `rg`/source inspection and record the limitation in the story record.
  - [x] If a prepared local environment is available, run the quickstart far enough to capture the first command status outcome; if infrastructure is unavailable, record the exact missing prerequisite and do not claim live execution.

## Dev Notes

### Source Context

- Epic 8 objective: developers can follow a validated quickstart, understand event contracts, see the reactive access demo, and design for timing, idempotency, and compensating commands. Story 8.1 owns the prerequisite-validated quickstart path. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 8: Developers Can Adopt Through Documentation and Demo Evidence`]
- Story 8.1 requires setup prerequisite validation, missing-prerequisite remediation before first command, restore/build/AppHost/first-command guidance, success/rejection interpretation without raw-log reading as the primary signal, and reviewer validation of commands, paths, packages, and outputs. [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.1: Create a Prerequisite-Validated Quickstart`]
- PRD FR59 requires a quickstart that enables first tenant command within 30 minutes. FR60 requires prerequisite validation for DAPR sidecar and EventStore deployment. The product brief also identifies prerequisite validation, DAPR setup guidance, NuGet discoverability, and time-to-first-command as adoption-critical. [Source: `_bmad-output/planning-artifacts/prd.md#Documentation & Adoption`; `_bmad-output/planning-artifacts/product-brief-Tenants-2026-03-06.md#Alex - Evaluate & Adopt`]
- Architecture maps Epic 8 work to `docs/`, `README.md`, and the sample project. AppHost/Aspire own local distributed topology; EventStore is the command gateway; DAPR remains the actor/state/pub-sub/service-invocation abstraction. [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`; `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]
- The 2026-05-31 implementation-readiness report marks Epic 8 implementation-ready and notes no readiness blocker for the documentation/demo epic. [Source: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-31.md#Epic Readiness Summary`]

### Current Repository State

- `docs/quickstart.md` already exists and covers .NET 10 SDK, DAPR CLI/runtime, Docker, the `system` tenant, root-level submodule initialization, Release build, AppHost launch, Keycloak token acquisition, HMAC fallback, Swagger UI command submission, command-status polling, tenant query verification, consumer event subscription, Testing package usage, and troubleshooting.
- `README.md` already links `docs/quickstart.md`, lists the five NuGet packages, links `docs/demo.md`, and shows the repo structure.
- `deploy/dapr/README.md` already documents local full `dapr init`, slim-mode prerequisites, production templates, DAPR component names, access-control posture, and failure triage.
- `src/Hexalith.Tenants.AppHost/Program.cs` starts Keycloak by default, wires EventStore as AppId `eventstore`, Tenants as AppId `tenants`, Admin services, the sample service, DAPR sidecars, shared state store, and pub/sub.
- `src/Hexalith.Tenants/appsettings.json` registers EventStore domain services for `system|tenants|v1` and `system|global-administrators|v1`, both pointing to AppId `tenants` method `process`.
- `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs` exposes query routes under `GET /api/tenants`, `GET /api/tenants/{tenantId}`, `GET /api/tenants/{tenantId}/users`, `GET /api/users/{userId}/tenants`, and `GET /api/tenants/{tenantId}/audit`.

### Previous Work Intelligence

- An archived old Story 8.1 created the first quickstart/README and recorded several superseded assumptions. Use it only as background. The current sprint Story 8.1 is stricter: it must validate prerequisites and evidence the reviewer path, not simply create the files. [Source: `_bmad-output/implementation-artifacts/archive/story-automator-20260601T143814Z-old-epics-7-9/8-1-quickstart-guide-and-readme.md`]
- Epic 8 retrospective warns that adoption docs need source verification gates and fresh-environment proof; do not claim full release readiness from source inspection alone. [Source: `_bmad-output/implementation-artifacts/epic-8-retro-2026-05-12.md#Action Items`]
- Story 7.2 added production DAPR templates and updated quickstart troubleshooting for full local init versus slim-mode prerequisites. Preserve those distinctions. [Source: `_bmad-output/implementation-artifacts/7-2-configure-dapr-components-for-local-and-production-deployment.md#Completion Notes List`]
- Story 7.5 hardened health/readiness semantics: `/alive` is process liveness, `/ready` is dependency readiness, and DAPR state-store readiness failures should be support-safe. Use those endpoint meanings consistently if the quickstart mentions health checks. [Source: `_bmad-output/implementation-artifacts/7-5-prove-stateless-operation-health-and-startup-reconstruction.md#Completion Notes List`]
- Recent commits show Epic 7 deployment/observability/auth/health work landing in order: Story 7.1 Aspire hosting, 7.2 DAPR components, 7.3 production authentication and EventStore tenant claims, 7.4 OpenTelemetry metrics, and 7.5 stateless readiness/reconstruction. Quickstart assumptions must reflect these latest local runtime changes.

### Latest Technical Notes

- Microsoft Learn's current Aspire DAPR integration guidance uses the DAPR hosting integration from the CommunityToolkit package family and local AppHost sidecars. Keep this story aligned to the repository-pinned `CommunityToolkit.Aspire.Hosting.Dapr` usage; do not switch package families or introduce `aspire new` templates. [Source: Microsoft Learn, Dapr integration for Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/frameworks/dapr)
- DAPR's current `dapr init` CLI reference documents that `--slim` excludes placement, scheduler, Redis, and Zipkin in self-hosted mode. That supports the quickstart's full-init requirement for actor/state/pub-sub local evaluation. [Source: Dapr Docs, init CLI command reference](https://docs.dapr.io/reference/cli/dapr-init/)
- DAPR's current self-hosted initialization docs state local initialization starts scheduler and Redis-backed default components. Keep local quickstart failure triage framed as prerequisite setup, not application-code failure. [Source: Dapr Docs, Initialize Dapr in your local environment](https://docs.dapr.io/getting-started/install-dapr-selfhost/)

### Technical Guardrails

- Use repo-pinned versions and package families: .NET SDK `10.0.300`, DAPR SDK `1.17.9`, `CommunityToolkit.Aspire.Hosting.Dapr 13.3.0-preview.1.260514-0647`, Aspire `13.3.x`, xUnit v3, Shouldly, and central package management. Do not bump dependencies for this documentation story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- Use `Hexalith.Tenants.slnx`; do not introduce `.sln` instructions.
- Initialize only root-level submodules: `Hexalith.EventStore`, `Hexalith.Commons`, `Hexalith.AI.Tools`, `Hexalith.Builds`, and `Hexalith.FrontComposer`. Never recommend `git submodule update --init --recursive`.
- The platform EventStore tenant is `system`; the tenant domain is `tenants`; global administrator domain and aggregate ID are `global-administrators`. Do not route global administrator commands as normal tenant-domain commands.
- The command gateway is EventStore-owned. Quickstart command examples must use `POST /api/v1/commands` through the EventStore service, not new Tenants-specific endpoints.
- DAPR component names are contracts: AppId `tenants`, state store `statestore`, pub/sub `pubsub`, topic `tenants.events`, dead-letter topic `deadletter.tenants.events`.
- Keep local auth separate from production auth. Local Keycloak and HMAC fallback examples are local-only; production OIDC setup belongs in the production auth docs.
- Do not require raw log reading as the main proof of success. Prefer command status, HTTP status, `rejectionEventType`, Aspire dashboard resource health, and query responses.
- Do not duplicate event contract, idempotency, cross-aggregate timing, or compensating-command documentation in the quickstart; link to the dedicated docs owned by later Epic 8 stories.

### Existing Files Likely to Touch

- `docs/quickstart.md`: primary update target.
- `README.md`: update only for quickstart link/time-estimate/package drift.
- `deploy/dapr/README.md`: reference, not a likely edit target unless quickstart links uncover stale headings.
- `docs/production-auth-claim-contract.md` and `docs/production-auth-readiness.md`: reference for production boundary; do not move production guidance into the quickstart.
- `_bmad-output/implementation-artifacts/tests/test-summary.md`: update only if this repository records manual validation evidence there for documentation stories.

### Testing Standards

- This is documentation work, but examples are executable user-facing contracts. Validate file paths, project names, command routes, package IDs, and expected output shapes against source before claiming completion.
- If code tests are needed because source examples expose a behavior gap, use xUnit v3 and Shouldly; do not add placeholder tests.
- Live AppHost/DAPR validation requires a prepared local environment. Missing Docker, DAPR, Redis, placement, scheduler, or Keycloak is a prerequisite gap to record, not a product failure.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 8.1: Create a Prerequisite-Validated Quickstart`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Documentation & Adoption`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`]
- [Source: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-05-31.md#Epic 8`]
- [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- [Source: `_bmad-output/project-context.md#Identity Scheme`]
- [Source: `_bmad-output/project-context.md#DAPR`]
- [Source: `docs/quickstart.md`]
- [Source: `README.md`]
- [Source: `deploy/dapr/README.md`]
- [Source: `src/Hexalith.Tenants.AppHost/Program.cs`]
- [Source: `src/Hexalith.Tenants/appsettings.json`]
- [Source: `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`]
- [Source: `docs/production-auth-claim-contract.md`]
- [Source: `docs/production-auth-readiness.md`]
- [Source: Microsoft Learn, Dapr integration for Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/frameworks/dapr)
- [Source: Dapr Docs, init CLI command reference](https://docs.dapr.io/reference/cli/dapr-init/)
- [Source: Dapr Docs, Initialize Dapr in your local environment](https://docs.dapr.io/getting-started/install-dapr-selfhost/)

### Project Structure Notes

- Alignment: Story 8.1 belongs in `docs/`, `README.md` if needed, and validation evidence. It should not change domain code, package topology, AppHost topology, or production DAPR templates unless source verification finds drift.
- Existing quickstart is not blank. The implementation should harden and verify it, especially prerequisite checks, local-only assumptions, and success/rejection interpretation.
- Detected conflict to resolve carefully: archived old Story 8.1 contains outdated version pins and superseded assumptions. Use current project context and source files as authoritative.
- Dirty worktree note at story creation: `_bmad-output/story-automator/orchestration-7-20260601-143204.md` already had unrelated local modifications. Do not restore or rewrite that file during Story 8.1 implementation unless the implementation explicitly needs it.

## Validation Checklist Results

- Story foundation: PASS. Epic 8 Story 8.1 acceptance criteria are preserved and expanded into implementation tasks.
- Architecture/source context: PASS. The story points to current docs, README, AppHost wiring, appsettings domain-service registrations, query routes, DAPR deployment guidance, production auth docs, and project context guardrails.
- Reinvention prevention: PASS. The story directs the developer to harden existing `docs/quickstart.md` and `README.md` instead of recreating docs or adding new runtime pathways.
- Wrong-library/version prevention: PASS. The story records current .NET, DAPR, Aspire, CommunityToolkit, xUnit, and package-management constraints and says not to bump dependencies.
- File-location prevention: PASS. The story limits expected changes to `docs/quickstart.md`, `README.md` if needed, and validation evidence.
- Regression prevention: PASS. The story keeps EventStore as the command gateway, preserves AppHost/DAPR topology, forbids recursive submodule init guidance, and separates local auth from production auth.
- Validation evidence: PASS. The story requires source verification and prepared-environment proof where available, with explicit instructions not to claim live execution when prerequisites are unavailable.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex (story authoring)

### Debug Log References

- Resolved `bmad-dev-story` workflow customization: no activation prepend/append steps; persistent fact `_bmad-output/project-context.md` loaded.
- Verified EventStore command route in `Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandsController.cs` and command-status route/shape in `CommandStatusController.cs`, `CommandStatusResponse.cs`, and `CommandStatus.cs`.
- Verified AppHost local topology in `src/Hexalith.Tenants.AppHost/Program.cs` and domain-service registrations in `src/Hexalith.Tenants/appsettings.json`.
- Verified query routes in `src/Hexalith.Tenants/Controllers/TenantsQueryController.cs`.
- `dotnet test` targeted run built successfully but VSTest aborted in this sandbox with `SocketException (13): Permission denied`; direct xUnit runner was used for execution.
- Docker API access is unavailable in this sandbox: `permission denied while trying to connect to the docker API at unix:///var/run/docker.sock`; live AppHost/first-command execution was not claimed.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Hardened `docs/quickstart.md` with prerequisite validation for pinned .NET SDK, full DAPR local runtime, Docker, root-level submodules, AppHost resource readiness, EventStore command gateway availability, token validation, and tenant-claim failures before first command submission.
- Verified and corrected first-command documentation against source: EventStore `POST /api/v1/commands`, AppHost project path, `BootstrapGlobalAdmin` then `CreateTenant`, ULID-shaped message IDs, matching `aggregateId`/`payload.TenantId`, command-status polling, `Completed` status code `4`, query verification, and structured rejection handling.
- Updated the local Keycloak realm so `admin-user` authorizes both quickstart command domains (`global-administrators` and `tenants`) and added regression coverage for that fixture.
- Updated README quickstart copy to match the 30-minute prerequisite-installed target and avoid a conflicting first-time setup estimate.
- Validation: Release build passed with 0 warnings and 0 errors; direct xUnit suite passed 1306 total, 0 failed, 26 skipped for DAPR/performance prerequisites.

### File List

- `README.md`
- `_bmad-output/implementation-artifacts/8-1-create-a-prerequisite-validated-quickstart.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `docs/quickstart.md`
- `src/Hexalith.Tenants.AppHost/KeycloakRealms/hexalith-realm.json`
- `tests/Hexalith.Tenants.Contracts.Tests/SolutionStructureTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs`
- `tests/Hexalith.Tenants.Server.Tests/Documentation/QuickstartDocumentationTests.cs`
- `tests/test-summary.md`

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approved after auto-fixes.

Findings fixed:

- [MEDIUM] Story File List omitted actual changed files: `tests/Hexalith.Tenants.Server.Tests/Documentation/QuickstartDocumentationTests.cs` and `tests/test-summary.md`. Updated the story File List so review evidence matches git reality.
- [MEDIUM] Quickstart validation claimed package-name and command-contract coverage, but `QuickstartDocumentationTests` only parsed JSON envelopes and did not deserialize payloads into the real command contracts. Added contract deserialization for `BootstrapGlobalAdmin`, `CreateTenant`, and `AddUserToTenant`, plus package-name/project checks for `Hexalith.Tenants.Contracts` and `Hexalith.Tenants.Client`.

Validation:

- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~QuickstartDocumentationTests -m:1 -nr:false` built successfully, then VSTest aborted in this sandbox with `SocketException (13): Permission denied`.
- `dotnet test tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~SolutionStructureTests -m:1 -nr:false` built successfully, then VSTest aborted in this sandbox with `SocketException (13): Permission denied`.
- `tests/Hexalith.Tenants.Server.Tests/bin/Release/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Documentation.QuickstartDocumentationTests` passed: 5 total, 0 failed, 0 skipped.
- `tests/Hexalith.Tenants.Contracts.Tests/bin/Release/net10.0/Hexalith.Tenants.Contracts.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Contracts.Tests.SolutionStructureTests` passed: 6 total, 0 failed, 0 skipped.
- `tests/Hexalith.Tenants.Server.Tests/bin/Release/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Configuration.EventPublicationConfigurationTests` passed: 17 total, 0 failed, 0 skipped.

## Change Log

| Date       | Version | Description | Author |
|------------|---------|-------------|--------|
| 2026-06-01 | 0.1 | Created Story 8.1 context for prerequisite-validated quickstart hardening and validation. | GPT-5 Codex |
| 2026-06-01 | 1.0 | Implemented prerequisite-validated quickstart hardening, local Keycloak claim alignment, source/static validation, and test evidence. | GPT-5 Codex |
| 2026-06-01 | 1.1 | Review auto-fixes: synchronized File List and strengthened quickstart command/package validation tests. | GPT-5 Codex |
