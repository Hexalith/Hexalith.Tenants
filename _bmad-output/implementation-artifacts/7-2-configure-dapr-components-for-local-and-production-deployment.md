---
baseline_commit: 25d53a3b037fa5a68bed43796f6e04ae7fd49ec6
---

# Story 7.2: Configure DAPR Components for Local and Production Deployment

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a platform operator,
I want standard DAPR component configuration for Tenants,
so that the tenant service can run beside EventStore using portable actors, state, pub/sub, and service invocation.

## Acceptance Criteria

1. Given local or production DAPR components are reviewed, when Tenants is deployed, then actor, state store, pub/sub, and service invocation configuration match the documented Tenants/EventStore conventions, and domain code does not directly depend on Redis, brokers, or databases.
2. Given DAPR access control is enabled, when service invocation paths are configured, then callers and receivers are explicit according to deny-by-default policy, and the Tenants domain processor route is reachable only through approved service invocation paths.
3. Given the domain processor route is configured, when EventStore aggregate actors invoke Tenants domain processing, then the Tenants host exposes the required processing endpoint, and the route participates in the normal authentication, authorization, telemetry, and error-handling pipeline where applicable.
4. Given DAPR slim or local mode is used, when operators follow the setup guidance, then placement and scheduler prerequisites are documented or validated, and actor startup failures point to the missing prerequisite rather than ambiguous command failures.
5. Given deployment configuration tests or AppHost diagnostics run, when DAPR components are missing or misnamed, then startup or diagnostics fail clearly, and the failure identifies the component or AppId mismatch.

## Tasks / Subtasks

- [x] Reconcile local AppHost DAPR component configuration against current Tenants/EventStore conventions. (AC: 1, 5)
  - [x] Keep local component names aligned with the runtime contract: AppIds `eventstore`, `tenants`, `eventstore-admin`, `eventstore-admin-ui`, `sample`; state store `statestore`; pub/sub `pubsub`; event topic `tenants.events`; dead-letter topic `deadletter.tenants.events`.
  - [x] Preserve `src/Hexalith.Tenants.AppHost/DaprComponents/statestore.yaml` as the local Redis-backed state store with `actorStateStore: "true"` and scopes for the app IDs that actually use state: `eventstore`, `tenants`, and `eventstore-admin`.
  - [x] Preserve `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` as local Redis-backed pub/sub with `enableDeadLetter: "true"`, `deadLetterTopic: "deadletter.tenants.events"`, and scopes for `eventstore` and `sample` only unless implementation evidence proves another app ID needs pub/sub.
  - [x] Make any local YAML comments and README references match files that exist in this repository; do not leave stale references to `deploy/dapr/` unless this story adds that directory.

- [x] Add or update production DAPR deployment templates for Tenants. (AC: 1, 2, 4, 5)
  - [x] Create `deploy/dapr/` in this repository if production templates do not already exist.
  - [x] Include a production state-store component template named `statestore` with `actorStateStore: "true"`, secret/environment placeholders, and explicit scopes for `eventstore`, `tenants`, and `eventstore-admin`.
  - [x] Include a production pub/sub component template named `pubsub` with `enableDeadLetter: "true"`, `deadLetterTopic: "deadletter.tenants.events"`, secret/environment placeholders, and explicit scopes for event publisher/subscriber app IDs.
  - [x] Include production resiliency guidance or a `resiliency.yaml` template that does not contradict the existing local `resiliency.yaml` retry/timeout intent.
  - [x] Do not add Redis, broker, database, or cloud-provider SDK references to Tenants domain packages; provider choice belongs in DAPR component YAML and deployment docs.
  - [x] If using EventStore's `Hexalith.EventStore/deploy/dapr` templates as a model, copy only the Tenants-relevant conventions into this repository; do not edit the EventStore submodule unless a separate EventStore change is explicitly required.

- [x] Harden DAPR service invocation access-control configuration. (AC: 2, 3, 5)
  - [x] Add receiver-specific production DAPR `Configuration` templates instead of applying one broad policy to every sidecar.
  - [x] For the Tenants receiver configuration, use `defaultAction: deny` and allow only the EventStore app ID to invoke the required Tenants internal routes: `POST /process` and `POST /project`.
  - [x] For EventStore and Admin receiver configurations, preserve the existing caller/receiver responsibilities; do not accidentally grant Tenants, Sample, or Admin UI access to broad EventStore command/query surfaces.
  - [x] Keep local development access control clearly labelled as local-only if it remains allow-by-default. Production templates must be deny-by-default.
  - [x] Do not require JWT bearer authorization on `/process` or `/project` unless tests prove EventStore service invocation sends a compatible authenticated request. DAPR sidecar access control is the caller gate for these internal routes today; ASP.NET auth remains required on public command/query endpoints.

- [x] Preserve and document the domain processor and projection route contracts. (AC: 1, 2, 3)
  - [x] Confirm `src/Hexalith.Tenants/Program.cs` still exposes `POST /process` for `DomainServiceRequest` and `POST /project` for `ProjectionRequest`.
  - [x] Confirm EventStore domain-service resolution still routes `system|tenants|v1` and `system|global-administrators|v1` to AppId `tenants`, method `process`, either by explicit registration or EventStore's convention fallback where valid.
  - [x] Confirm EventStore projection invocation can reach the Tenants `/project` endpoint through approved DAPR service invocation.
  - [x] Keep `/process` and `/project` behind correlation/error-handling/telemetry middleware where applicable, but do not move aggregate business rules or projection write policy into AppHost/DAPR YAML.

- [x] Add setup and failure-triage guidance for DAPR local, slim, and production modes. (AC: 4, 5)
  - [x] Document that normal local development should run full `dapr init` so Redis, placement, and scheduler are available.
  - [x] Document that slim self-hosted mode requires operators to provide placement, scheduler, and state/pub-sub components themselves before actor flows can work.
  - [x] Document the expected local ports used by existing tests: Redis `localhost:6379`, placement `50005` on Linux or `6050` on Windows, scheduler `50006` on Linux or `6060` on Windows.
  - [x] Add failure triage for missing state store, missing pub/sub, missing placement, missing scheduler, wrong AppId, wrong component name, wrong component scope, and denied service invocation.

- [x] Add deterministic configuration validation tests. (AC: 1, 2, 4, 5)
  - [x] Add YAML/static tests in an existing test project that already uses `YamlDotNet`, likely `tests/Hexalith.Tenants.Server.Tests/Configuration`, to validate local and production component names, metadata, scopes, and deny-by-default access control.
  - [x] Assert the Tenants receiver access-control template allows `eventstore` to call only `POST /process` and `POST /project`.
  - [x] Assert production templates use placeholders for secrets and do not contain real connection strings, tokens, passwords, or private hostnames.
  - [x] Assert local comments or docs do not point to missing deployment files.
  - [x] Reuse existing `DaprFactAttribute`, `DaprTestPrerequisites`, and `AspireTopologyFixture` patterns for any live DAPR/AppHost checks; do not make live DAPR, Docker, Redis, Keycloak, placement, or scheduler mandatory for deterministic YAML tests.

- [x] Run focused validation and record evidence accurately. (AC: 1-5)
  - [x] Run `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release --filter FullyQualifiedName~Configuration`.
  - [x] Run any added focused AppHost/application-model tests if this story changes `src/Hexalith.Tenants.AppHost/Program.cs` or `src/Hexalith.Tenants.Aspire`.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --configuration Release`.
  - [x] If live DAPR/AppHost smoke tests are attempted, record missing prerequisites as blocked environment diagnostics, not as passing deployment evidence.

## Dev Notes

### Source Context

- Epic 7 objective: operators can deploy Tenants alongside EventStore with Aspire/DAPR, production auth validation, telemetry, health, stateless operation, and recovery behavior. Story 7.2 is the DAPR component and service-invocation configuration story. [Source: `_bmad-output/planning-artifacts/epics.md#Epic 7: Operators Can Deploy, Secure, and Observe Production Tenants`]
- Story 7.2 requires standard DAPR component configuration, deny-by-default service invocation, Tenants domain processor route reachability, DAPR local/slim prerequisite clarity, and clear startup/diagnostic failures for missing or misnamed components. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.2: Configure DAPR Components for Local and Production Deployment`]
- PRD FR56 requires operators to deploy Tenants alongside EventStore with standard DAPR configuration. NFR15/NFR16 require DAPR pub/sub and state-store abstraction rather than direct broker/database coupling. NFR22/NFR23 tie this work to health and durable recovery evidence that later Epic 7 stories expand. [Source: `_bmad-output/planning-artifacts/prd.md#Functional Requirements`; `_bmad-output/planning-artifacts/prd.md#Non-Functional Requirements`]
- Architecture maps Epic 7 work to `AppHost`, `Aspire`, `ServiceDefaults`, `Telemetry`, `Health`, `Configuration`, `Validation`, auth tests, production auth docs, and deployment smoke tests; DAPR is the only infrastructure abstraction for actors, state, pub/sub, and service invocation. [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`; `_bmad-output/planning-artifacts/architecture.md#External Integrations`]

### Current Repository State

- `src/Hexalith.Tenants.AppHost/Program.cs` resolves local access-control files from `DaprComponents`, adds `eventstore`, `eventstore-admin`, `eventstore-admin-ui`, `tenants`, optional `keycloak`, and `sample`, and calls `builder.AddHexalithTenants(tenants, accessControlConfigPath)`.
- `Program.cs` wires the EventStore sidecar with AppId `eventstore`, the Tenants sidecar with AppId `tenants`, Admin.Server with AppId `eventstore-admin`, Admin.UI with AppId `eventstore-admin-ui`, and Sample with AppId `sample`. EventStore, Tenants, and Admin.Server share the state store; EventStore, Tenants, and Sample use pub/sub only where explicitly referenced.
- `src/Hexalith.Tenants.Aspire/HexalithTenantsExtensions.cs` creates DAPR component resources from `HexalithTenantsAspireOptions`, attaches `actorStateStore=true` and `redisHost` metadata to `statestore`, creates `pubsub`, and wires the Tenants sidecar with dynamic DAPR ports.
- `src/Hexalith.Tenants.Aspire/HexalithTenantsAspireOptions.cs` defaults to AppId `tenants`, state store `statestore`, pub/sub `pubsub`, component type `state.redis`, and Redis host `localhost:6379`; validation rejects empty, whitespace, and malformed component type values.
- `src/Hexalith.Tenants.AppHost/DaprComponents/statestore.yaml` is local Redis state with `actorStateStore: "true"` and scopes `eventstore`, `tenants`, and `eventstore-admin`.
- `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` is local Redis pub/sub with `enableDeadLetter: "true"`, `deadLetterTopic: "deadletter.tenants.events"`, and scopes `eventstore` and `sample`.
- `src/Hexalith.Tenants.AppHost/DaprComponents/accesscontrol.yaml` is explicitly local-only and currently `defaultAction: allow`; its comment points to `deploy/dapr/`, but this repository does not currently contain a root `deploy/` directory.
- `src/Hexalith.Tenants.AppHost/DaprComponents/accesscontrol.eventstore-admin.yaml` is Admin.Server-specific local access control and also allow-by-default.
- `src/Hexalith.Tenants.AppHost/DaprComponents/resiliency.yaml` already contains local retry, timeout, and circuit-breaker settings for `eventstore`, `pubsub`, and `statestore`.
- `src/Hexalith.Tenants/Program.cs` maps `POST /process` and `POST /project` after correlation, exception handling, CloudEvents, authentication, and authorization middleware registration. The minimal endpoints are not currently decorated with `RequireAuthorization()`.
- `src/Hexalith.Tenants/appsettings.json` contains explicit domain-service registrations for `system|tenants|v1` and `system|global-administrators|v1` pointing to AppId `tenants` and method `process`; it also preserves `EventStore:Publisher:PubSubName = pubsub`, `global-administrators -> tenants.events`, and `Snapshots:DomainIntervals:tenants = 50`.
- `tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs` already parses local `pubsub.yaml` and `statestore.yaml` with `YamlDotNet` and asserts current topic/scoping expectations. Extend this style before introducing a new parser/test harness.

### Previous Story Intelligence

- Story 7.1 created the validated Aspire options model and application-model tests. Build on `HexalithTenantsAspireOptions`; do not reintroduce hard-coded duplicate DAPR topology constants into `AppHost/Program.cs`.
- Story 7.1 intentionally left production DAPR access-control hardening and component deployment templates to Story 7.2. [Source: `_bmad-output/implementation-artifacts/7-1-provide-aspire-hosting-extensions-for-tenants.md#Out of Scope`]
- Story 7.1 preserved dynamic DAPR sidecar ports; do not add fixed `DaprHttpPort` or `DaprGrpcPort` defaults unless a targeted diagnostic test proves the need. [Source: `_bmad-output/implementation-artifacts/7-1-provide-aspire-hosting-extensions-for-tenants.md#Preserve Existing Behavior`]
- Story 7.1 validation used direct xUnit executable fallback when VSTest socket creation was blocked. Keep runner-environment failures separate from application failures in this story record. [Source: `_bmad-output/implementation-artifacts/7-1-provide-aspire-hosting-extensions-for-tenants.md#Debug Log References`]
- Epic 5 retrospective explicitly warns that DAPR component names are implementation contracts and stale names like `tenants-eventstore` mislead future work. Use `statestore`, `pubsub`, `tenants.events`, and `deadletter.tenants.events`. [Source: `_bmad-output/implementation-artifacts/epic-5-retro-2026-06-01.md#Key Learnings`]
- Epic 6 retrospective warns not to infer hosted runtime confidence from in-memory fake tests. Story 7.2 must prove configuration directly through YAML/static tests and optionally prepared-environment runtime checks. [Source: `_bmad-output/implementation-artifacts/epic-6-retro-2026-06-01.md#Next Epic Preview`]

### Technical Guardrails

- Current version pins: .NET SDK `10.0.300`; DAPR SDK `1.17.9`; `CommunityToolkit.Aspire.Hosting.Dapr 13.3.0-preview.1.260514-0647`; `Aspire.Hosting 13.3.5`; `Aspire.Hosting.Testing 13.3.5`. Do not bump versions for this story unless a verified compatibility blocker requires it. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- Default DAPR/EventStore conventions are AppId `tenants`, state-store component `statestore`, pub/sub component `pubsub`, event topic `tenants.events`, dead-letter topic `deadletter.tenants.events`, platform tenant `system`, tenant domain `tenants`, and global-administrator domain `global-administrators`. [Source: `_bmad-output/project-context.md#DAPR`; `_bmad-output/project-context.md#Identity Scheme`]
- DAPR components must stay infrastructure configuration. Do not add provider SDKs, direct Redis clients, database clients, broker clients, or connection-string parsing to `Hexalith.Tenants.Server`, `.Contracts`, `.Client`, `.Testing`, or domain aggregate code.
- `Hexalith.Tenants.Aspire` remains hosting composition only. It must not reference host, server, domain, command, query, auth, projection, or test projects. [Source: `tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs#Aspire_package_exposes_hosting_composition_only`]
- Preserve central package management. Do not add inline `Version=` attributes to `PackageReference` entries. [Source: `_bmad-output/project-context.md#Package Management`]
- Use `Hexalith.Tenants.slnx`, file-scoped namespaces, K&R braces, nullable-aware code, xUnit v3, and Shouldly for tests. [Source: `_bmad-output/project-context.md#Language-Specific Rules`; `_bmad-output/project-context.md#Testing Rules`]

### Latest Technical Notes

- Microsoft's current Aspire DAPR guidance uses `CommunityToolkit.Aspire.Hosting.Dapr`; it requires the DAPR CLI and `dapr init` for local use, adds sidecars with `WithDaprSidecar`, and states the sidecar AppId defaults to the resource name unless customized. Keep Tenants' explicit AppId assertion instead of relying on an incidental resource name. [Source: Microsoft Learn, Dapr integration for Aspire: https://learn.microsoft.com/en-us/dotnet/aspire/community-toolkit/dapr]
- Aspire DAPR docs state actor state stores need `actorStateStore` metadata and that DAPR actors require exactly one actor state store. This is why `statestore.yaml` and production templates must keep `actorStateStore: "true"` on exactly the intended state-store component. [Source: Microsoft Learn, Dapr integration for Aspire: https://learn.microsoft.com/en-us/dotnet/aspire/community-toolkit/dapr]
- DAPR service invocation access control is configured on the called application sidecar. DAPR's current access-control documentation states that when app-specific policies exist and no global default is specified, DAPR assumes a secure deny default; this story should be explicit anyway and set `defaultAction: deny` in production templates. [Source: DAPR Docs, Apply access control list configuration for service invocation: https://docs.dapr.io/operations/configuration/invoke-allowlist/]
- DAPR component docs support multiple named state stores and pub/sub components, and component names are the application-facing contract. Tenants must keep `statestore` and `pubsub` stable unless every caller, test, and doc is migrated together. [Source: DAPR Docs, State stores components: https://docs.dapr.io/operations/components/setup-state-store/; DAPR Docs, Pub/sub brokers: https://docs.dapr.io/operations/components/setup-pubsub/]
- DAPR `dapr init --slim` excludes placement, scheduler, Redis, and Zipkin from self-hosted installation; full `dapr init` includes Redis, placement, and scheduler for local development. Story 7.2 diagnostics/docs should name those prerequisites directly. [Source: DAPR Docs, init CLI command reference: https://docs.dapr.io/reference/cli/dapr-init/; DAPR Docs, Initialize DAPR locally: https://docs.dapr.io/getting-started/install-dapr-selfhost/]
- DAPR placement is required for actor location in self-hosted mode, and the scheduler is started by full `dapr init` and can be run manually in slim mode. Existing tests already probe placement/scheduler ports; reuse that failure wording. [Source: DAPR Docs, Placement service overview: https://v1-15.docs.dapr.io/concepts/dapr-services/placement/; DAPR Docs, Scheduler service overview: https://docs.dapr.io/concepts/dapr-services/scheduler/]

### Existing Files to Touch

- `src/Hexalith.Tenants.AppHost/DaprComponents/statestore.yaml`: update only if comments, metadata, or scopes need correction; preserve name `statestore` and `actorStateStore: "true"`.
- `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml`: update only if comments, metadata, or scopes need correction; preserve name `pubsub` and dead-letter topic `deadletter.tenants.events`.
- `src/Hexalith.Tenants.AppHost/DaprComponents/accesscontrol.yaml`: likely update local comments and/or local policy clarity; if production deny-by-default config is added elsewhere, keep this local-only file unambiguous.
- `src/Hexalith.Tenants.AppHost/DaprComponents/accesscontrol.eventstore-admin.yaml`: update only if local Admin.Server access-control guidance is stale.
- `src/Hexalith.Tenants.AppHost/DaprComponents/resiliency.yaml`: update only if production/local docs need alignment; do not broaden runtime behavior casually.
- `deploy/dapr/*`: likely new production DAPR templates and README for Tenants.
- `docs/quickstart.md` or a focused deployment doc such as `docs/dapr-deployment.md`: update only enough to make local/slim/production DAPR setup and failure triage discoverable.
- `tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs` or a new configuration test file in the same folder: likely place for deterministic YAML validation.
- `tests/Hexalith.Tenants.IntegrationTests/HexalithTenantsAspireExtensionTests.cs`: update only if `Hexalith.Tenants.Aspire` options or AppHost resource wiring changes.

### Preserve Existing Behavior

- EventStore remains the command gateway and aggregate actor host. Tenants remains the domain service handling `/process` and `/project`.
- Tenants must continue to publish global-administrator events on the shared `tenants.events` topic while preserving `global-administrators` as the domain.
- Local dynamic DAPR sidecar ports must remain dynamic in AppHost.
- The Sample service is a pub/sub subscriber only; do not grant it state-store access.
- Admin.UI invokes Admin.Server; it should not gain direct state-store or Tenants domain-processor access.
- Do not alter query endpoint authorization, production JWT validation, telemetry instrumentation, health readiness semantics, stateless restart behavior, or pub/sub catch-up implementation unless a narrow configuration change requires a supporting test.

### Out of Scope

- Production JWT/OIDC validation and `eventstore:tenant=system` claim smoke tests. Story 7.3 and 7.6A own that work.
- OpenTelemetry command/event/query metrics implementation. Story 7.4 owns that work.
- Health/readiness endpoint semantics, stateless reconstruction, multi-instance proof, and snapshot benchmark execution. Story 7.5 owns that work.
- Full DAPR component and service-invocation smoke-test automation in a live environment. Story 7.6B owns the broader smoke-test evidence; Story 7.2 should provide deterministic config validation and any narrowly scoped diagnostics needed now.
- Pub/sub outage recovery and catch-up proof. Story 7.6D owns that work.
- Full deployment readiness checklist/evidence template. Story 7.6E owns the release-facing checklist.

### Testing Standards

- Use xUnit v3 and Shouldly; do not add `Assert.*` in new tests.
- Prefer deterministic YAML/static tests for component names, scopes, access-control routes, secret placeholders, and local-vs-production posture.
- Use `YamlDotNet` where YAML parsing is needed; it is already used by Tenants configuration tests.
- Live DAPR/Aspire tests must be prerequisite-gated using existing fixture patterns. Do not turn missing Docker/DAPR/Redis/placement/scheduler into ordinary test failures unless the test is explicitly a prepared-environment smoke test.
- Minimum expected evidence: focused configuration tests plus Release build. Add AppHost/application-model tests only if AppHost or Aspire topology code changes.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.2: Configure DAPR Components for Local and Production Deployment`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Functional Requirements`]
- [Source: `_bmad-output/planning-artifacts/prd.md#Non-Functional Requirements`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]
- [Source: `_bmad-output/project-context.md#DAPR`]
- [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]
- [Source: `_bmad-output/implementation-artifacts/7-1-provide-aspire-hosting-extensions-for-tenants.md`]
- [Source: `_bmad-output/implementation-artifacts/epic-5-retro-2026-06-01.md#Key Learnings`]
- [Source: `_bmad-output/implementation-artifacts/epic-6-retro-2026-06-01.md#Next Epic Preview`]
- [Source: `src/Hexalith.Tenants.AppHost/Program.cs`]
- [Source: `src/Hexalith.Tenants.Aspire/HexalithTenantsExtensions.cs`]
- [Source: `src/Hexalith.Tenants.Aspire/HexalithTenantsAspireOptions.cs`]
- [Source: `src/Hexalith.Tenants.AppHost/DaprComponents/statestore.yaml`]
- [Source: `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml`]
- [Source: `src/Hexalith.Tenants.AppHost/DaprComponents/accesscontrol.yaml`]
- [Source: `src/Hexalith.Tenants.AppHost/DaprComponents/accesscontrol.eventstore-admin.yaml`]
- [Source: `src/Hexalith.Tenants.AppHost/DaprComponents/resiliency.yaml`]
- [Source: `src/Hexalith.Tenants/Program.cs`]
- [Source: `src/Hexalith.Tenants/appsettings.json`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/Fixtures/DaprFactAttribute.cs`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs`]
- [Source: Microsoft Learn, Dapr integration for Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/community-toolkit/dapr)
- [Source: DAPR Docs, Apply access control list configuration for service invocation](https://docs.dapr.io/operations/configuration/invoke-allowlist/)
- [Source: DAPR Docs, State stores components](https://docs.dapr.io/operations/components/setup-state-store/)
- [Source: DAPR Docs, Pub/sub brokers](https://docs.dapr.io/operations/components/setup-pubsub/)
- [Source: DAPR Docs, init CLI command reference](https://docs.dapr.io/reference/cli/dapr-init/)
- [Source: DAPR Docs, Initialize DAPR locally](https://docs.dapr.io/getting-started/install-dapr-selfhost/)
- [Source: DAPR Docs, Placement service overview](https://v1-15.docs.dapr.io/concepts/dapr-services/placement/)
- [Source: DAPR Docs, Scheduler service overview](https://docs.dapr.io/concepts/dapr-services/scheduler/)

## Project Structure Notes

- Alignment: Story 7.2 belongs in AppHost/DAPR deployment configuration, production deployment templates, focused docs, and deterministic configuration tests.
- Detected variance: local `accesscontrol.yaml` references `deploy/dapr/`, but the Tenants repository currently has no root `deploy/` directory. Either add the production templates in this story or remove/reword the stale reference.
- Detected risk: local allow-by-default access-control can mask route-policy mistakes. Production templates must be deny-by-default and receiver-specific.
- Detected route boundary: Tenants internal `/process` and `/project` endpoints are reached by EventStore service invocation. Treat DAPR sidecar access control as the current caller gate unless endpoint-auth tests prove a stronger authenticated invocation path.
- No UX/frontend changes are required for this story.

## Dev Agent Record

### Agent Model Used

Codex GPT-5

### Debug Log References

- 2026-06-01: Added deterministic configuration tests first, then verified the focused `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Release --filter FullyQualifiedName~Configuration` path built but VSTest aborted before execution with sandbox `System.Net.Sockets.SocketException (13): Permission denied`.
- 2026-06-01: Retried focused validation with `MSBUILDDISABLENODEREUSE=1 ... -m:1 /nr:false /p:BuildInParallel=false`; the project built, then VSTest again aborted on socket setup. Used direct xUnit runner fallback.
- 2026-06-01: Focused direct xUnit validation passed: `tests/Hexalith.Tenants.Server.Tests/bin/Release/net10.0/Hexalith.Tenants.Server.Tests -noLogo -noColor -parallel none -class Hexalith.Tenants.Server.Tests.Configuration.EventPublicationConfigurationTests` -> 16 total, 0 failed, 0 skipped.
- 2026-06-01: Release solution build passed: `MSBUILDDISABLENODEREUSE=1 dotnet build Hexalith.Tenants.slnx --configuration Release --no-restore -m:1 /nr:false /p:BuildInParallel=false /p:UseSharedCompilation=false` -> 0 warnings, 0 errors.
- 2026-06-01: Full direct xUnit regression passed after VSTest socket denial: Contracts 105/0 failed, Client 92/0 failed, Testing 181/0 failed, Sample 31/0 failed, Server 650/0 failed, Integration 202 total with 0 failed and 25 expected DAPR/performance prerequisite skips.
- 2026-06-01: Searched project/package references for direct Redis, database, broker, or cloud-provider SDK additions; none were introduced.
- 2026-06-01: Senior review tightened deterministic assertions for Tenants internal route middleware order, production pub/sub scoping metadata, and Tenants receiver caller exclusivity; test project build passed and focused direct xUnit validation passed: 16 total, 0 failed, 0 skipped.

### Completion Notes List

- Added production DAPR templates under `deploy/dapr` for the `statestore` and `pubsub` component contracts, receiver-specific access-control, resiliency, and deployment guidance.
- Preserved local component names/scopes and clarified local-only access-control comments without changing AppHost topology code.
- Documented full local `dapr init`, slim-mode operator responsibilities, expected ports, and failure triage for missing state store, pub/sub, placement, scheduler, wrong AppId, wrong component name, wrong scope, and denied service invocation.
- Extended deterministic YAML/static configuration tests to lock component names, metadata, scopes, production deny-by-default access control, Tenants `/process` and `/project` routes, domain-service registrations, placeholder-only secrets, and DAPR setup guidance.

### File List

- `_bmad-output/implementation-artifacts/7-2-configure-dapr-components-for-local-and-production-deployment.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/story-automator/orchestration-7-20260601-143204.md`
- `deploy/dapr/README.md`
- `deploy/dapr/accesscontrol.eventstore-admin.yaml`
- `deploy/dapr/accesscontrol.eventstore.yaml`
- `deploy/dapr/accesscontrol.tenants.yaml`
- `deploy/dapr/pubsub.yaml`
- `deploy/dapr/resiliency.yaml`
- `deploy/dapr/statestore.yaml`
- `docs/quickstart.md`
- `src/Hexalith.Tenants.AppHost/DaprComponents/accesscontrol.eventstore-admin.yaml`
- `src/Hexalith.Tenants.AppHost/DaprComponents/accesscontrol.yaml`
- `tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs`

### Change Log

- 2026-06-01: Added production DAPR component, access-control, resiliency, and deployment guidance templates for Story 7.2.
- 2026-06-01: Added deterministic configuration tests for local/production DAPR contracts, Tenants receiver access control, route/domain-service contracts, secret placeholders, and setup guidance.
- 2026-06-01: Updated local DAPR comments and quickstart troubleshooting to distinguish full local DAPR init, slim mode prerequisites, and production deny-by-default posture.
- 2026-06-01: Senior review fixed test gaps for production pub/sub scoped publishing/subscription, Tenants receiver caller exclusivity, and internal route middleware-order evidence.

### Senior Developer Review (AI)

Outcome: Approved after automatic fixes. Reviewed the story file, git changes, AppHost DAPR YAML, production DAPR templates, quickstart guidance, Tenants host routes, appsettings domain-service registrations, deterministic configuration tests, project context, architecture notes, and current DAPR documentation for service-invocation ACLs, actor state-store metadata, and slim/full init prerequisites.

Findings fixed:

- MEDIUM: `ProductionTenantsAccessControl_AllowsEventStoreOnlyForInternalRoutes` did not prove EventStore was the only allowed caller. Added an exact policy AppId assertion.
- MEDIUM: production pub/sub tests did not lock `publishingScopes` and `subscriptionScopes`, so a future change could let `sample` publish or subscribe broadly while tests still passed. Added explicit metadata assertions.
- MEDIUM: Tenants internal route coverage only checked string presence, not that `/process` and `/project` were mapped after correlation, exception handling, CloudEvents, authentication, and authorization middleware. Added ordering assertions.

Remaining risk: live DAPR/AppHost smoke proof is still out of scope for Story 7.2 and remains dependent on prepared runtime infrastructure.

## Validation Checklist Results

- Story foundation extracted from Epic 7 Story 7.2 and kept aligned to FR56, NFR15, NFR16, NFR22, and NFR23.
- PRD/architecture requirements incorporated: DAPR as the infrastructure abstraction, AppHost/Aspire ownership of topology, package boundaries, and deployment-vs-domain separation.
- Previous story intelligence incorporated from Story 7.1: reuse Aspire options, preserve dynamic DAPR ports, keep production DAPR hardening in this story, and separate deterministic tests from live topology evidence.
- Current repository files inspected for likely touch points: AppHost `Program.cs`, Tenants Aspire extension/options, local DAPR component YAML, Tenants host endpoints, appsettings, existing YAML tests, and DAPR prerequisite fixtures.
- Disaster-prevention checks added: avoid direct provider coupling in domain code, avoid stale component names, avoid broad production service invocation, avoid applying one access-control file to every receiver, avoid claiming live runtime readiness from static tests, and avoid breaking EventStore invocation by adding endpoint auth without proof.
- Senior developer review: PASS after automatic fixes; no critical issues remain.
- Definition of Done: PASS for story-context creation.
- Story Review Complete: `7-2-configure-dapr-components-for-local-and-production-deployment`.
