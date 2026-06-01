---
baseline_commit: 43016d344e0534281f50a68f0196317812fb323e
---

# Story 7.1: Provide Aspire Hosting Extensions for Tenants

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer deploying Tenants,
I want Aspire hosting extensions for the tenant service,
so that I can add Tenants to an AppHost through the documented Hexalith integration path.

## Acceptance Criteria

1. Given an AppHost references `Hexalith.Tenants.Aspire`, when the developer calls the tenant hosting extension, then the Tenants service is added to the distributed application model and the extension returns the expected Aspire builder type for fluent composition.
2. Given the hosting extension is configured with default options, when the AppHost is built, then the Tenants host uses the documented AppId, domain, state store, pub/sub topic, and service invocation conventions and no inline duplicated AppHost wiring is required in consumer applications.
3. Given the hosting extension is configured with custom deployment options, when valid options are supplied, then options are validated consistently with the project configuration pattern and invalid options fail early with actionable errors.
4. Given the package boundary is reviewed, when the Aspire package is inspected, then it exposes hosting composition only and it does not own tenant domain rules or command handling behavior.
5. Given Aspire extension tests run, when default and configured AppHost setups are exercised, then tests verify resource names, service references, DAPR sidecar wiring, and package boundaries.

## Tasks / Subtasks

- [x] Define the Aspire options surface without breaking the existing extension call path (AC: 1, 3)
  - [x] Add a small options type in `src/Hexalith.Tenants.Aspire`, such as `HexalithTenantsAspireOptions`, with defaults for `AppId = "tenants"`, `StateStoreName = "statestore"`, `PubSubName = "pubsub"`, `DaprConfigPath = null`, and Redis/local state-store metadata currently used by the extension.
  - [x] Add validation that rejects null/empty/whitespace names and invalid component settings before Aspire build execution proceeds.
  - [x] Preserve the current public call shape `builder.AddHexalithTenants(tenants, accessControlConfigPath)` or provide a compatible overload so existing AppHost code continues to compile.
  - [x] Add an overload that accepts `Action<HexalithTenantsAspireOptions>` or an options instance for custom deployment settings.

- [x] Refactor the Aspire extension implementation around the options model (AC: 1, 2, 3, 4)
  - [x] Keep the extension in `src/Hexalith.Tenants.Aspire/HexalithTenantsExtensions.cs`.
  - [x] Keep returning `HexalithTenantsResources` for fluent composition; do not return `IDistributedApplicationBuilder` only if that would remove access to the created state store, pub/sub, or Tenants project resource.
  - [x] Keep `HexalithTenantsResources` limited to Aspire resource builders and rename `CommandApi` only if needed to clarify it is the Tenants domain service project.
  - [x] Continue wiring a DAPR sidecar for the Tenants project with AppId `tenants` by default.
  - [x] Continue creating/referencing DAPR components named `statestore` and `pubsub` by default.
  - [x] Ensure actor state-store metadata remains present for the state store; DAPR actors require exactly one actor state store.
  - [x] Do not add aggregate, command, query, auth, projection, or domain validation behavior to the Aspire package.

- [x] Keep the AppHost using the packaged extension rather than inline duplicate topology (AC: 2)
  - [x] Update `src/Hexalith.Tenants.AppHost/Program.cs` only where needed to pass the new options while preserving existing EventStore, admin, Keycloak, sample, and DAPR access-control wiring.
  - [x] Preserve the EventStore publisher topic override `EventStore__Publisher__TopicOverrides__global-administrators = tenants.events`.
  - [x] Preserve dynamic DAPR sidecar ports; do not reintroduce fixed `DaprHttpPort` or `DaprGrpcPort` defaults.
  - [x] Preserve static DAPR access-control config resolution and fail-fast error text for missing config files.

- [x] Add application-model tests for the Aspire extension (AC: 1, 2, 3, 5)
  - [x] Add focused tests that build a minimal `IDistributedApplicationBuilder`, add a stub/project resource, call the Tenants Aspire extension, and inspect resource names and annotations without requiring DAPR, Docker, Redis, or live sidecars.
  - [x] Verify default names: Tenants AppId `tenants`, state store `statestore`, pub/sub `pubsub`, and returned resource builders.
  - [x] Verify configured options override valid names and config path values.
  - [x] Verify invalid options fail early with `ArgumentException`, `InvalidOperationException`, or options-validation-style exceptions that identify the bad setting without exposing secrets.
  - [x] Verify the extension does not add host-only or domain package dependencies to `Hexalith.Tenants.Aspire`.

- [x] Align tests with current repository standards (AC: 5)
  - [x] Use xUnit v3 and Shouldly; do not add new `Assert.*` calls.
  - [x] Replace or avoid copying the existing `ScaffoldingSmokeTests` `Assert.True(true)` pattern; it is a placeholder and does not meet current test quality.
  - [x] Keep full runtime Aspire/DAPR smoke tests in `tests/Hexalith.Tenants.IntegrationTests` but do not make Story 7.1 depend on live topology unless the environment is available.
  - [x] If a new test project is added, follow central package management and solution structure conventions; no inline package versions.

- [x] Run focused validation and record blocked diagnostics accurately (AC: 1-5)
  - [x] Run `dotnet build Hexalith.Tenants.slnx --configuration Release`.
  - [x] Run the focused Aspire extension/application-model tests.
  - [x] Run existing package governance tests that validate the five published packages and package boundaries.
  - [x] If live Aspire/DAPR runtime tests are attempted, ensure `dapr init` prerequisites are present and record environment failures as blocked, not passed.

## Dev Notes

### Source Context

- Epic 7 objective: operators can deploy Tenants alongside EventStore with Aspire/DAPR, production auth validation, telemetry, health, stateless operation, and recovery behavior. Story 7.1 is the package/AppHost composition entry point for that epic. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 7: Operators Can Deploy, Secure, and Observe Production Tenants`]
- Story 7.1 specifically requires Aspire hosting extensions, default topology conventions, custom option validation, package-boundary checks, and extension tests. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.1: Provide Aspire Hosting Extensions for Tenants`]
- PRD FR48 requires deployment through .NET Aspire hosting extensions; FR56 requires deployment alongside EventStore using standard DAPR configuration; FR57/NFR12 require stateless operation, which later Epic 7 stories prove at runtime. [Source: `_bmad-output/planning-artifacts/prd.md#Developer Experience & Packaging`; `_bmad-output/planning-artifacts/prd.md#Observability & Operations`; `_bmad-output/planning-artifacts/prd.md#Scalability`]
- Architecture maps Epic 7 work to `AppHost`, `Aspire`, `ServiceDefaults`, `Telemetry`, `Health`, `Configuration`, `Validation`, auth tests, production auth docs, and deployment smoke tests. [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]

### Current Repository State

- `src/Hexalith.Tenants.Aspire/HexalithTenantsExtensions.cs` already exposes `AddHexalithTenants(this IDistributedApplicationBuilder builder, IResourceBuilder<ProjectResource> tenants, string? daprConfigPath = null)`. It creates `statestore` with `state.redis` metadata, creates `pubsub`, attaches a DAPR sidecar to the Tenants project, sets AppId `tenants`, and returns `HexalithTenantsResources`.
- `src/Hexalith.Tenants.Aspire/HexalithTenantsResources.cs` currently returns `StateStore`, `PubSub`, and `CommandApi`; despite the property name, the resource is the Tenants domain service project passed by AppHost.
- `src/Hexalith.Tenants.AppHost/Program.cs` creates projects for `eventstore`, `eventstore-admin`, `eventstore-admin-ui`, `tenants`, and `sample`; it calls `builder.AddHexalithTenants(tenants, accessControlConfigPath)`, then reuses returned state store/pub-sub resources for EventStore, Admin.Server, and Sample sidecars.
- `src/Hexalith.Tenants.AppHost/DaprComponents/statestore.yaml` and `pubsub.yaml` are the static local component templates. They use component names `statestore` and `pubsub`, Redis metadata, actor state-store metadata, and dead-letter topic `deadletter.tenants.events`.
- `src/Hexalith.Tenants.AppHost/DaprComponents/accesscontrol.yaml` is currently local-development permissive by default but still defines app IDs that Story 7.2 will harden. Story 7.1 should preserve the config path plumbing and avoid changing access-control policy semantics.
- `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs` verifies live resource liveness and `/process` dispatch through the AppHost fixture, but these are runtime smoke tests, not focused unit/application-model tests of the Aspire extension.
- `tests/Hexalith.Tenants.IntegrationTests/ScaffoldingSmokeTests.cs` is a placeholder using `Assert.True(true)`. Do not copy this pattern into new tests; new tests should use Shouldly and prove real behavior.

### Technical Guardrails

- Current version pins: .NET SDK `10.0.300`; `Aspire.Hosting` and `Aspire.Hosting.Testing` `13.3.5`; AppHost SDK `Aspire.AppHost.Sdk/13.3.3`; `CommunityToolkit.Aspire.Hosting.Dapr` `13.3.0-preview.1.260514-0647`; DAPR SDK `1.17.9`. Do not bump versions in this story unless a verified compatibility blocker requires it. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`; `Directory.Packages.props`]
- `Hexalith.Tenants.Aspire` is one of the five published NuGet packages. Keep it as hosting composition only: `Aspire.Hosting` plus `CommunityToolkit.Aspire.Hosting.Dapr`; no dependency on `Hexalith.Tenants.Server`, the host project, domain aggregates, controllers, auth pipeline, or tests. [Source: `_bmad-output/planning-artifacts/prd.md#NuGet Package Architecture`; `src/Hexalith.Tenants.Aspire/Hexalith.Tenants.Aspire.csproj`]
- Default resource conventions are AppId `tenants`, state-store component `statestore`, topic `tenants.events`, dead-letter topic `deadletter.tenants.events`, and actor identity `system:tenants:{aggregateId}`. Do not introduce `commandapi`, `tenants-eventstore`, per-event topics, or direct Redis/database/broker coupling in domain packages. [Source: `_bmad-output/planning-artifacts/architecture.md#Naming Patterns`; `_bmad-output/project-context.md#DAPR`]
- The Aspire extension can create resource composition; it must not own tenant command handling, aggregate rules, projection writes, auth decisions, or event contracts. [Source: `_bmad-output/planning-artifacts/architecture.md#Architectural Boundaries`]
- Preserve central package management. Do not add `Version=` to any `PackageReference`. [Source: `_bmad-output/project-context.md#Package Management`]
- Use `Hexalith.Tenants.slnx`, file-scoped namespaces, K&R braces, nullable-aware code, and one type per file. [Source: `_bmad-output/project-context.md#Language-Specific Rules`]

### Existing Files to Touch

- `src/Hexalith.Tenants.Aspire/HexalithTenantsExtensions.cs`: current extension creates DAPR components and sidecar inline. This is the primary update file.
- `src/Hexalith.Tenants.Aspire/HexalithTenantsResources.cs`: update only if property naming or returned resource shape needs clarification; preserve consumer access to state store, pub/sub, and Tenants project builders.
- `src/Hexalith.Tenants.Aspire/HexalithTenantsAspireOptions.cs` or similar: likely new options file for validated custom deployment settings.
- `src/Hexalith.Tenants.AppHost/Program.cs`: update only to use the new options overload while preserving current topology.
- `tests/Hexalith.Tenants.IntegrationTests/*` or a new focused test file/project: add application-model tests for the extension and keep runtime tests as separate evidence.
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs` or existing package governance scripts: extend only if package-boundary checks do not already cover the Aspire package dependency shape.

### Preserve Existing Behavior

- EventStore remains the command gateway resource with AppId `eventstore`; Tenants remains the domain service resource with AppId `tenants`.
- The AppHost must keep the global-administrator publisher topic override so `global-administrators` events publish to shared topic `tenants.events` while preserving their aggregate domain.
- DAPR sidecar ports are intentionally dynamic. Fixed ports caused prior local conflicts and should not return.
- AppHost project metadata classes under `src/Hexalith.Tenants.AppHost/*.cs` replaced generated `Projects` namespace assumptions in recent commit `786d582 refactor(apphost): replace Projects namespace classes`; do not undo that pattern.
- Story 1.4 already proved package-reference consumer compilation for the Aspire package. Story 7.1 should build on that by improving and testing the actual hosting extension behavior, not by replacing package validation scripts.

### Previous Epic Intelligence

- Epic 6 retrospective carry-forward: Testing package evidence is aggregate/fake parity only. It does not prove host auth, DAPR routing, actor runtime, AppHost topology, or Data Protection deployment behavior. Story 7.1 must prove hosted composition directly at the Aspire model level and leave runtime smoke proof to Epic 7 runtime stories where appropriate. [Source: `_bmad-output/implementation-artifacts/epic-6-retro-2026-06-01.md#Next Epic Preview`]
- Epic 5 retrospective carry-forward: keep DAPR component naming aligned to implementation: `statestore`, `pubsub`, `tenants.events`, and `deadletter.tenants.events`. Planning drift around `tenants-eventstore` previously misled projection work. [Source: `_bmad-output/implementation-artifacts/epic-5-retro-2026-06-01.md#Action Items`]
- Story 4.1 and Epic 4 established that global-administrator events must publish on shared `tenants.events` while retaining `global-administrators` as their command/projection domain. Do not collapse the domain distinction in AppHost or Aspire package code. [Source: `_bmad-output/implementation-artifacts/4-1-publish-tenant-domain-events-as-cloudevents.md#Dev Notes`; `_bmad-output/implementation-artifacts/epic-4-retro-2026-06-01.md#Key Learnings`]

### Latest Technical Notes

- Current Aspire DAPR guidance uses `CommunityToolkit.Aspire.Hosting.Dapr`, `WithDaprSidecar`, DAPR sidecar options, DAPR component resources, and component references from sidecars. DAPR CLI plus `dapr init` are runtime prerequisites for local DAPR execution, but application-model tests can remain compile/model focused. [Source: Microsoft Learn, Dapr integration for Aspire: https://learn.microsoft.com/en-us/dotnet/aspire/frameworks/dapr]
- Aspire DAPR docs state that DAPR sidecars appear as separate dashboard resources and AppId defaults to the resource name unless customized. Story 7.1 should assert the explicit AppId `tenants` rather than relying on incidental resource naming. [Source: Microsoft Learn, Dapr integration for Aspire: https://learn.microsoft.com/en-us/dotnet/aspire/frameworks/dapr]
- Aspire testing uses `Aspire.Hosting.Testing` and `DistributedApplicationTestingBuilder` for AppHost lifecycle tests. Runtime tests should wait for resources with a timeout and create clients by resource name; existing `AspireTopologyFixture` already follows that pattern. [Source: Microsoft Learn, Aspire testing overview: https://learn.microsoft.com/en-us/dotnet/aspire/testing/overview; https://learn.microsoft.com/en-us/dotnet/aspire/testing/accessing-resources]

### Testing Standards

- Use xUnit v3 and Shouldly. Do not add `Assert.*` in new test files.
- Prefer application-model tests for default/custom/invalid option behavior so this story is not blocked by DAPR, Docker, Redis, Keycloak, or live EventStore.
- Keep Tier 3 runtime tests opt-in or prerequisite-gated when they require `dapr init`, Docker, Redis, placement, or scheduler.
- Test names should describe the behavior in repository style, for example `AddHexalithTenants_DefaultOptions_CreateExpectedDaprResources`.
- Minimum evidence: Release build, focused Aspire extension tests, and package-boundary/governance tests for the Aspire package.

### Out of Scope

- Production DAPR access-control hardening and component deployment templates beyond preserving the existing config path. Story 7.2 owns DAPR component and service invocation deployment configuration.
- Production JWT/OIDC validation and `eventstore:tenant=system` smoke tests. Story 7.3 and 7.6A own that work.
- OpenTelemetry metrics and structured-log instrumentation. Story 7.4 owns that work.
- Health/readiness, stateless restart, startup reconstruction benchmarks, and snapshot tuning. Story 7.5 owns that work.
- Documentation/adoption quickstart updates unless a small note is needed to explain the Aspire extension usage. Epic 8 owns full adoption docs.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.1: Provide Aspire Hosting Extensions for Tenants`]
- [Source: `_bmad-output/planning-artifacts/prd.md#NuGet Package Architecture`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Developer Experience & Packaging`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Infrastructure & Deployment`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`]
- [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- [Source: `_bmad-output/project-context.md#DAPR`]
- [Source: `_bmad-output/project-context.md#Extension Methods (DI Registration)`]
- [Source: `src/Hexalith.Tenants.Aspire/HexalithTenantsExtensions.cs`]
- [Source: `src/Hexalith.Tenants.Aspire/HexalithTenantsResources.cs`]
- [Source: `src/Hexalith.Tenants.AppHost/Program.cs`]
- [Source: `src/Hexalith.Tenants.AppHost/DaprComponents/statestore.yaml`]
- [Source: `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs`]
- [Source: Microsoft Learn, Dapr integration for Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/frameworks/dapr)
- [Source: Microsoft Learn, Aspire testing overview](https://learn.microsoft.com/en-us/dotnet/aspire/testing/overview)
- [Source: Microsoft Learn, Access resources in Aspire tests](https://learn.microsoft.com/en-us/dotnet/aspire/testing/accessing-resources)

## Project Structure Notes

- Alignment: Story 7.1 belongs in `src/Hexalith.Tenants.Aspire` and AppHost/tests around hosting composition. It should not touch domain aggregates, projections, query controllers, Client event handlers, Testing fakes, or Phase 2 UI artifacts.
- Detected variance: the Aspire extension currently creates Redis-backed DAPR components using hard-coded metadata, while static AppHost DAPR YAML also exists under `src/Hexalith.Tenants.AppHost/DaprComponents`. The story should keep names aligned and make deployment customization explicit through options rather than scattering topology constants.
- Detected test gap: current runtime Aspire tests prove liveness and `/process` dispatch, but they do not inspect the extension's application model for default names, custom options, invalid options, or package-boundary behavior.
- Dirty worktree note at story creation: `_bmad-output/implementation-artifacts` already had deleted legacy Epic 7-9 story files and an archive folder created by prior story-automator activity. Do not restore or rewrite those unrelated changes during implementation.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release --filter "Category=ApplicationModel"` could not execute under this sandbox because VSTest attempted to open a denied TCP socket.
- `MSBUILDDISABLENODEREUSE=1 dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release -m:1 /nr:false` passed.
- `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Release/net10.0/Hexalith.Tenants.IntegrationTests.dll -trait "Category=ApplicationModel" -noLogo -parallel none` passed: 19 total, 0 failed.
- `dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Release/net10.0/Hexalith.Tenants.Contracts.Tests.dll -method "Hexalith.Tenants.Contracts.Tests.PackageGovernanceTests.Aspire_package_exposes_hosting_composition_only" -noLogo -parallel none` passed: 1 total, 0 failed.
- `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -m:1 /nr:false` passed with 0 warnings and 0 errors.
- In-process full regression run passed: Contracts 105/0 failed, Client 92/0 failed, Testing 181/0 failed, Sample 31/0 failed, Server 643/0 failed, Integration 192/0 failed with 25 skipped due unavailable DAPR/performance prerequisites.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Aspire MCP documentation calls were unavailable in this session, so latest notes use official Microsoft Learn Aspire documentation discovered through web search plus repository-pinned project context.
- Added `HexalithTenantsAspireOptions` with default AppId/component names, DAPR config path, state-store component type, Redis host metadata, and fail-fast validation.
- Refactored `AddHexalithTenants` to route the compatible string overload, an `Action<HexalithTenantsAspireOptions>` overload, and an options-instance overload through the validated options model.
- Preserved default Tenants DAPR sidecar AppId `tenants`, default `statestore` and `pubsub` components, actor state-store metadata, Redis host metadata, dynamic DAPR sidecar ports, and `HexalithTenantsResources` fluent return shape.
- Added focused Aspire application-model tests for default topology, sidecar/component references, custom options, invalid option validation, and package-boundary governance without requiring live DAPR/Docker/Redis.
- Senior developer review auto-fixed stricter DAPR option validation, callback-overload null-argument ordering, and stale Redis state-store API documentation.

### File List

- `_bmad-output/implementation-artifacts/7-1-provide-aspire-hosting-extensions-for-tenants.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `src/Hexalith.Tenants.Aspire/HexalithTenantsAspireOptions.cs`
- `src/Hexalith.Tenants.Aspire/HexalithTenantsExtensions.cs`
- `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj`
- `tests/Hexalith.Tenants.IntegrationTests/HexalithTenantsAspireExtensionTests.cs`

### Change Log

- 2026-06-01: Added validated Tenants Aspire options and overloads, refactored extension topology wiring to use options, added application-model and package-boundary tests, and validated Release build plus in-process regression suite.
- 2026-06-01: Senior developer review auto-fixed option validation gaps, callback-overload fail-fast behavior, and stale extension documentation; reran focused validation successfully.

### Senior Developer Review (AI)

Reviewer: GPT-5 Codex on 2026-06-01

Outcome: Approve after auto-fixes.

Findings fixed:

- MEDIUM: `AddHexalithTenants(..., Action<HexalithTenantsAspireOptions>)` validated `configureOptions` before `builder` and `tenants`, allowing callback side effects before fail-fast argument validation. Fixed by validating all required arguments before invoking the callback and adding regression tests.
- MEDIUM: DAPR app/component option validation only rejected empty or whitespace-only strings, so embedded-whitespace resource identifiers could flow into Aspire/DAPR model creation. Fixed by rejecting whitespace in AppId, component names, component type, and Redis host metadata, and by requiring DAPR component type format `category.provider`.
- LOW: Extension API documentation still described the state store as in-memory even though the implementation provisions Redis-backed DAPR state. Fixed the XML documentation to match behavior.

Review validation:

- Story status verified as reviewable before review and set to `done` after fixes.
- No dedicated story-context or Epic 7 tech-spec file was found; review used the story, `epics.md`, PRD, architecture, and project-context files.
- Acceptance Criteria 1-5 cross-checked against `Hexalith.Tenants.Aspire`, AppHost usage, application-model tests, and package-governance tests.
- File List matched the relevant changed implementation/test files; git also contains story-automator orchestration artifacts outside application source, which were excluded from code review per workflow instructions.
- Aspire MCP doc search was attempted but unavailable in this session; official Microsoft Learn Aspire DAPR/testing documentation was used as fallback.
- `MSBUILDDISABLENODEREUSE=1 dotnet build tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Release -m:1 /nr:false` passed.
- `dotnet tests/Hexalith.Tenants.IntegrationTests/bin/Release/net10.0/Hexalith.Tenants.IntegrationTests.dll -trait "Category=ApplicationModel" -noLogo -parallel none` passed: 19 total, 0 failed.
- `dotnet tests/Hexalith.Tenants.Contracts.Tests/bin/Release/net10.0/Hexalith.Tenants.Contracts.Tests.dll -method "Hexalith.Tenants.Contracts.Tests.PackageGovernanceTests.Aspire_package_exposes_hosting_composition_only" -noLogo -parallel none` passed: 1 total, 0 failed.
- `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release -m:1 /nr:false` passed with 0 warnings and 0 errors.

## Validation Checklist Results

- Story foundation extracted from Epic 7 Story 7.1 and expanded into concrete implementation tasks.
- PRD/architecture requirements incorporated: five-package boundary, Aspire hosting composition, DAPR resource conventions, EventStore source-of-truth boundaries, and runtime proof separation.
- Current repository files inspected for likely touch points: Aspire extension/resources, AppHost `Program.cs`, DAPR component YAML, runtime Aspire tests, Aspire topology fixture, package project files, package governance references, quickstart/event docs, Epic 5/6 retrospectives, and recent git history.
- Disaster-prevention checks added: avoid duplicate AppHost wiring, avoid domain behavior in the Aspire package, preserve AppId/resource names, avoid fixed sidecar ports, use Shouldly, and separate application-model tests from live DAPR runtime evidence.
- Definition of Done: PASS
- Story Review Complete: 7-1-provide-aspire-hosting-extensions-for-tenants
- Completion Score: 27/27 required story checkboxes completed
- Quality Gates: Release build passed; focused application-model tests passed; package governance passed; full in-process regression suite passed with live DAPR/performance tests prerequisite-gated.
- Test Results: 1244 total tests passed across in-process assemblies, 25 integration/performance tests skipped due unavailable DAPR/performance prerequisites, 0 failed.
- Documentation: File List, Debug Log References, Completion Notes, Change Log, story status, and sprint status updated.
