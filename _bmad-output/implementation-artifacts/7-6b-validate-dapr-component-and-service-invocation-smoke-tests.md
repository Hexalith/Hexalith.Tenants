---
baseline_commit: 4db3ca7
---

# Story 7.6B: Validate DAPR Component and Service Invocation Smoke Tests

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a platform operator,
I want DAPR component and service invocation smoke tests,
so that tenant command processing can reach required EventStore and Tenants service paths safely.

## Acceptance Criteria

1. Given DAPR component smoke tests run, when actor, state store, pub/sub, placement, scheduler, and service invocation inputs are missing or misnamed, then the failure identifies the missing deployment input or dependency, and it does not produce ambiguous runtime errors or leak secrets.
2. Given the domain processor route is smoke-tested, when EventStore aggregate actors invoke Tenants domain processing, then the required service invocation path succeeds only through approved DAPR configuration, and deny-by-default service invocation assumptions are preserved.

## Tasks / Subtasks

- [x] Reconcile existing DAPR topology evidence before changing code. (AC: 1, 2)
  - [x] Read Story 7.2, Story 7.5, and Story 7.6A completion notes; treat existing DAPR templates, deterministic YAML tests, readiness behavior, and auth smoke coverage as baseline.
  - [x] Confirm local AppHost component names and AppIds remain `eventstore`, `tenants`, `eventstore-admin`, `eventstore-admin-ui`, `sample`, `statestore`, `pubsub`, `tenants.events`, and `deadletter.tenants.events`.
  - [x] Confirm production templates under `deploy/dapr` remain receiver-specific and deny-by-default, especially `accesscontrol.tenants.yaml` allowing only `eventstore` to `POST /process` and `POST /project`.
  - [x] Do not duplicate Story 7.6A auth tests, Story 7.6C health/readiness tests, or Story 7.6D pub/sub recovery tests.

- [x] Add deterministic DAPR smoke-contract validation for missing or misnamed deployment inputs. (AC: 1)
  - [x] Extend `tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs` or add a focused configuration test in the same folder using existing `YamlDotNet` helpers.
  - [x] Assert local and production component files identify missing/misnamed state store, pub/sub, actor state-store metadata, scopes, placement, scheduler, wrong AppId, wrong component name, wrong component scope, and denied service invocation in docs or test diagnostics.
  - [x] Assert `deploy/dapr/README.md` and `docs/quickstart.md` keep live-smoke prerequisites explicit: full `dapr init` for local mode; slim mode requires externally provided Redis/state store, pub/sub, placement, scheduler, and correct component names/scopes.
  - [x] Assert production templates and docs do not include concrete secrets, connection strings, bearer tokens, compact JWTs, decoded token payloads, real private hosts, tenant IDs, user IDs, or PII.
  - [x] Keep static validation deterministic and infrastructure-free; do not make Docker, Redis, DAPR CLI, Aspire, or Keycloak required for this lane.

- [x] Harden live DAPR prerequisite diagnostics without masking product failures. (AC: 1)
  - [x] Reuse `DaprFactAttribute`, `DaprTestPrerequisites`, and `TenantsDaprTestFixture`; do not create another DAPR fixture unless a specific limitation is documented.
  - [x] If fixture diagnostics are extended, preserve explicit checks for Redis `localhost:6379`, placement `50005` on Linux or `6050` on Windows, and scheduler `50006` on Linux or `6060` on Windows.
  - [x] Ensure unavailable infrastructure is recorded as a prerequisite skip or blocked environment diagnostic, not as passing smoke evidence.
  - [x] Ensure real DAPR startup/runtime failures after prerequisites pass are not broadly converted into skips by substring matching.
  - [x] Keep diagnostics support-safe: name failed dependency categories and ports only; do not print secrets, tokens, full command payloads, decoded JWTs, or raw production connection details.

- [x] Prove Tenants domain processor service invocation through the DAPR actor command path. (AC: 2)
  - [x] Extend `tests/Hexalith.Tenants.IntegrationTests/DaprEndToEndTests.cs` only if current coverage does not already prove `AggregateActor -> DAPR service invocation -> Tenants /process -> DomainServiceRequestHandler -> TenantAggregate`.
  - [x] Preserve or add a smoke assertion that a `CreateTenant` command submitted through the aggregate actor succeeds end to end and persists/publishes one `TenantCreated` event through the configured `pubsub` topic.
  - [x] Preserve or add evidence that `system|tenants|v1` and `system|global-administrators|v1` domain-service registrations point to AppId `tenants` and method `process`; do not route global administrators through a separate topic or AppId.
  - [x] If testing a denied service-invocation path is practical, prove that callers other than `eventstore` cannot invoke Tenants internal routes under the production deny-by-default template. If not practical in local sidecar tests, keep this as deterministic YAML evidence and record the live-environment boundary.
  - [x] Do not require JWT bearer auth on `/process` or `/project` unless EventStore service invocation actually sends compatible authenticated requests; DAPR receiver access control is the current internal caller gate.

- [x] Keep AppHost/Aspire smoke boundaries explicit. (AC: 1, 2)
  - [x] Review `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs`; preserve its documented liveness-only boundary unless a new test explicitly checks DAPR behavior.
  - [x] Use `AspireTopologyTests` only for prepared-environment smoke evidence that goes through the AppHost resource model; do not reinterpret `/alive` checks as DAPR component readiness.
  - [x] If adding AppHost smoke checks, use existing Aspire fixture patterns and prerequisite checks for Docker, Redis, placement, and scheduler.
  - [x] Preserve dynamic DAPR sidecar ports in `src/Hexalith.Tenants.AppHost/Program.cs`; do not introduce fixed DAPR ports to make tests easier.

- [x] Capture support-safe smoke-test evidence. (AC: 1, 2)
  - [x] Update `_bmad-output/implementation-artifacts/tests/test-summary.md` or a narrowly named 7.6B evidence artifact with command, test class/filter, pass/fail/skip counts, safe dependency categories, and date.
  - [x] Record static YAML validation separately from live DAPR/AppHost smoke evidence; static validation is not live deployment proof.
  - [x] If live DAPR prerequisites are unavailable in the developer environment, record the exact prerequisite skip reason and do not mark live smoke AC evidence as passed from static checks alone.

- [x] Run focused validation and record evidence accurately. (AC: 1, 2)
  - [x] Run `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~Configuration"`.
  - [x] Run `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~DaprEndToEndTests|FullyQualifiedName~AspireTopologyTests"`.
  - [x] If VSTest socket restrictions occur in this sandbox, use the repository's direct xUnit executable fallback pattern and record the environment limitation separately from product failures.
  - [x] Run `dotnet build Hexalith.Tenants.slnx --configuration Debug --no-restore` if production code, AppHost, Aspire topology, DAPR YAML, docs, project files, or shared test fixtures change.
  - [x] Do not mark ACs complete from skipped live tests or from static YAML validation alone; record the remaining deployment evidence boundary.

## Dev Notes

### Source Context

- Epic 7 objective: operators can deploy Tenants alongside EventStore with Aspire/DAPR, production auth validation, telemetry, health, stateless operation, and recovery behavior. Story 7.6B is the DAPR component and service-invocation smoke-test slice of the corrected deployment-readiness story. [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.6B: Validate DAPR Component and Service Invocation Smoke Tests`]
- The 2026-05-31 sprint correction split the old oversized Story 7.6 into auth, DAPR/service invocation, health/readiness, pub/sub recovery, and final evidence-template stories so each failure mode can be tested and diagnosed independently. [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-31.md#Proposal B: Split Story 7.6 Before Handoff`]
- PRD/epics map this story to FR56 deployment beside EventStore with standard DAPR configuration, NFR15/NFR16 infrastructure abstraction through DAPR, NFR17 pub/sub reliability boundaries, NFR22 availability evidence, and NFR23 durable recovery evidence. Story 7.6B owns component and service-invocation smoke evidence, not auth, health readiness, or pub/sub recovery. [Source: `_bmad-output/planning-artifacts/epics.md#Functional Requirements`; `_bmad-output/planning-artifacts/epics.md#NFR Coverage Map`]
- Architecture maps Epic 7 to `AppHost`, `Aspire`, `ServiceDefaults`, `Telemetry`, `Health`, `Configuration`, `Validation`, auth tests, production auth docs, and deployment smoke tests. DAPR is the only infrastructure abstraction for actors, state, pub/sub, and service invocation. [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`; `_bmad-output/planning-artifacts/architecture.md#External Integrations`]

### Current Repository State

- `src/Hexalith.Tenants.AppHost/Program.cs` resolves local access-control files from `DaprComponents`, adds `eventstore`, `eventstore-admin`, `eventstore-admin-ui`, `tenants`, optional `keycloak`, and `sample`, and wires Tenants through `builder.AddHexalithTenants(tenants, accessControlConfigPath)`.
- The AppHost uses `CommunityToolkit.Aspire.Hosting.Dapr`; EventStore sidecar AppId is `eventstore`, Tenants AppId is `tenants`, Admin.Server AppId is `eventstore-admin`, Admin.UI AppId is `eventstore-admin-ui`, and Sample AppId is `sample`. Local and production YAML scope `statestore` to `eventstore`, `tenants`, and `eventstore-admin`; current pub/sub templates scope publishing/subscription to `eventstore` and `sample`, while the Aspire extension still exposes the `PubSub` resource for composition.
- `src/Hexalith.Tenants.Aspire/HexalithTenantsExtensions.cs` creates DAPR component resources from `HexalithTenantsAspireOptions`, adds `actorStateStore=true` and `redisHost` metadata to `statestore`, creates `pubsub`, and attaches the Tenants sidecar without fixed DAPR ports.
- `src/Hexalith.Tenants.Aspire/HexalithTenantsAspireOptions.cs` defaults to AppId `tenants`, state store `statestore`, pub/sub `pubsub`, component type `state.redis`, and Redis host `localhost:6379`; validation rejects empty, whitespace, and malformed component type values.
- `src/Hexalith.Tenants.AppHost/DaprComponents/statestore.yaml` is local Redis state with `actorStateStore: "true"` and scopes `eventstore`, `tenants`, and `eventstore-admin`.
- `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml` is local Redis pub/sub with `enableDeadLetter: "true"`, `deadLetterTopic: "deadletter.tenants.events"`, and scopes `eventstore` and `sample`.
- `src/Hexalith.Tenants.AppHost/DaprComponents/accesscontrol.yaml` is explicitly local-only and allow-by-default for developer ergonomics; production receiver-specific deny-by-default templates live under `deploy/dapr`.
- `deploy/dapr/accesscontrol.tenants.yaml` is the production Tenants receiver policy. It uses `defaultAction: deny` and allows only AppId `eventstore` to call `POST /process` and `POST /project`.
- `deploy/dapr/README.md` already documents local full `dapr init`, slim-mode responsibilities, production component scopes, and failure triage for missing placement, scheduler, state store, pub/sub, wrong AppId, wrong component name, wrong component scope, and denied service invocation.
- `src/Hexalith.Tenants/appsettings.json` explicitly registers `system|tenants|v1` and `system|global-administrators|v1` domain services with AppId `tenants` and method `process`, and configures `EventStore:Publisher:PubSubName = pubsub`.
- `src/Hexalith.Tenants/Program.cs` maps `POST /process` for `DomainServiceRequest` and `POST /project` for `ProjectionRequest` after correlation, exception handling, CloudEvents, authentication, and authorization middleware registration. These internal endpoints are not currently decorated with `RequireAuthorization()`.
- `tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs` already parses local and production DAPR YAML with `YamlDotNet`, asserts component names/scopes, production deny-by-default access control, route/domain-service contracts, secret placeholders, and setup guidance.
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs` starts a Tenants test host with a local `daprd` sidecar and real EventStore server infrastructure. It checks Redis, placement, and scheduler prerequisites, starts `daprd`, waits for sidecar health, maps `/process`, and creates temporary `statestore`/`pubsub` component files.
- `tests/Hexalith.Tenants.IntegrationTests/DaprEndToEndTests.cs` already exercises full actor command paths through DAPR sidecar, aggregate actor, service invocation to `/process`, aggregate processing, event persistence, and pub/sub publication. It is `[DaprFact]`-gated.
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs` starts the full Aspire AppHost when Docker and DAPR prerequisites are available, but its comments intentionally classify the fixture as process liveness evidence; full DAPR readiness belongs in DAPR-specific tests.

### Previous Story Intelligence

- Story 7.1 established Aspire hosting extension options and dynamic DAPR sidecar port behavior. Do not reintroduce hard-coded DAPR topology constants in AppHost tests or fixed sidecar ports in production code. [Source: `_bmad-output/implementation-artifacts/7-1-provide-aspire-hosting-extensions-for-tenants.md`]
- Story 7.2 created production DAPR templates and deterministic YAML/static tests. Story 7.6B should smoke-test and package evidence from those contracts, not rewrite the DAPR deployment model. [Source: `_bmad-output/implementation-artifacts/7-2-configure-dapr-components-for-local-and-production-deployment.md`]
- Story 7.3 and Story 7.6A own production JWT/OIDC and `eventstore:tenant=system` smoke evidence. Keep 7.6B focused on DAPR components and service invocation; do not weaken the fail-closed auth behavior. [Source: `_bmad-output/implementation-artifacts/7-3-validate-production-authentication-and-eventstore-tenant-claims.md`; `_bmad-output/implementation-artifacts/7-6a-validate-production-auth-smoke-tests.md`]
- Story 7.5 hardened `/ready` to depend on the DAPR state store and explicitly kept EventStore service-invocation readiness out of `/ready`; the DAPR end-to-end tests and deployment smoke lane prove service invocation separately. Preserve that evidence boundary. [Source: `_bmad-output/implementation-artifacts/7-5-prove-stateless-operation-health-and-startup-reconstruction.md`]
- Story 7.6A completed immediately before this story. Its latest commit is `4db3ca7 feat(story-7.6A): Validate Production Auth Smoke Tests`; it recorded VSTest socket denial in this sandbox and used direct xUnit runner fallback successfully. Use the same environment reporting pattern. [Source: `git log --oneline -n 8`; `_bmad-output/implementation-artifacts/7-6a-validate-production-auth-smoke-tests.md`]

### Git Intelligence

- Latest relevant commit before story creation: `4db3ca7 feat(story-7.6A): Validate Production Auth Smoke Tests`. Treat this as the baseline for completed auth smoke work.
- Current worktree at story creation has an unrelated modification in `_bmad-output/story-automator/orchestration-7-20260602-053838.md`. Do not restore, rewrite, or claim ownership of that file during 7.6B implementation unless the user explicitly routes work through story-automator.

### Latest Technical Information

- Microsoft Aspire DAPR guidance continues to use `CommunityToolkit.Aspire.Hosting.Dapr`; local use requires the DAPR CLI and `dapr init`, and sidecars are added with `WithDaprSidecar`. Keep the repository-pinned integration and explicit AppIds rather than depending on incidental resource names. [Source: Microsoft Learn, Dapr integration for Aspire, `https://learn.microsoft.com/en-us/dotnet/aspire/community-toolkit/dapr`]
- Aspire/DAPR actor guidance says actor state stores require `actorStateStore` metadata and DAPR actors require exactly one actor state store. This supports the existing `statestore` contract and the deterministic test that exactly one component has `actorStateStore: "true"`. [Source: Microsoft Learn, Dapr integration for Aspire, `https://learn.microsoft.com/en-us/dotnet/aspire/frameworks/dapr`]
- DAPR service invocation access control is configured on the called application's sidecar. DAPR docs show explicit `defaultAction: deny` plus per-AppId allowed operations; this matches the production Tenants receiver policy for `eventstore -> POST /process` and `POST /project`. [Source: DAPR Docs, Apply access control list configuration for service invocation, `https://docs.dapr.io/operations/configuration/invoke-allowlist/`]
- DAPR `dapr init --slim` excludes placement, scheduler, Redis, and Zipkin from self-hosted installation; full `dapr init` pulls Redis, placement, and scheduler. Keep 7.6B diagnostics explicit about those local prerequisites. [Source: DAPR Docs, init CLI command reference, `https://docs.dapr.io/reference/cli/dapr-init/`]
- Use repo-pinned versions: .NET SDK `10.0.300`, DAPR SDK `1.17.9`, `CommunityToolkit.Aspire.Hosting.Dapr 13.3.0-preview.1.260514-0647`, Aspire `13.3.x`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and NSubstitute `6.0.0-rc.1`. Do not upgrade packages for this story. [Source: `_bmad-output/project-context.md#Technology Stack & Versions`]

### Technical Guardrails

- DAPR component names are application contracts: `statestore`, `pubsub`, `tenants.events`, and `deadletter.tenants.events`. Do not introduce `tenants-eventstore`, per-event topics, per-tenant state-store names, or new provider-specific package references.
- EventStore remains the command gateway and aggregate actor host. Tenants remains the domain service for `/process` and `/project`. Do not host `AggregateActor` inside Tenants production host.
- Keep provider choices in DAPR YAML and deployment docs. Do not add Redis, broker, database, cloud-provider, or connection-string parsing dependencies to `Hexalith.Tenants.Contracts`, `.Client`, `.Server`, `.Testing`, or domain aggregate code.
- Internal `/process` and `/project` routes are reached through approved DAPR service invocation. Public command/query routes continue to use ASP.NET authentication/authorization and EventStore validators.
- Static YAML validation proves configuration contracts only. Live smoke evidence requires prepared DAPR infrastructure and should be recorded separately.
- Missing local infrastructure should produce explicit skip/blocker diagnostics naming Redis, placement, scheduler, state store, pub/sub, AppId, component scope, or denied service invocation. It must not be recorded as a passing smoke test.
- Preserve support-safe evidence. Do not emit compact JWTs, signing keys, decoded token payloads, secret placeholders resolved to values, command payloads, real issuer URLs, real tenant/user IDs, connection strings, or PII in docs, assertions, skip reasons, logs, or story evidence.

### Existing Files Likely to Touch

- `tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs`: likely place for deterministic DAPR smoke-contract assertions and documentation drift checks.
- `tests/Hexalith.Tenants.IntegrationTests/DaprEndToEndTests.cs`: likely place to harden or label live service-invocation smoke coverage if current assertions are insufficient.
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs`: update only if prerequisite diagnostics or support-safe failure classification need tightening.
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/DaprFactAttribute.cs`: update only if prerequisite messaging needs a narrow improvement.
- `tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs` and `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs`: update only for prepared-environment AppHost smoke evidence; do not broaden liveness semantics casually.
- `deploy/dapr/README.md` and `docs/quickstart.md`: update only if failure triage or smoke evidence instructions are stale.
- `_bmad-output/implementation-artifacts/tests/test-summary.md` or a focused Story 7.6B evidence artifact: record support-safe static and live smoke evidence.

### Preserve Existing Behavior

- Preserve AppHost resource names, AppIds, dynamic DAPR sidecar ports, component names, local/production YAML file locations, and package boundaries.
- Preserve production deny-by-default receiver-specific access-control templates.
- Preserve `accesscontrol.yaml` local-only allow-by-default posture unless the change explicitly updates local developer behavior and proves AppHost tests still pass.
- Preserve the Sample service as a pub/sub subscriber only; do not grant it state-store access.
- Preserve Admin.UI -> Admin.Server -> EventStore responsibilities; do not grant Admin.UI direct Tenants domain-processor access.
- Preserve Tenants health/readiness semantics from Story 7.5; do not make `/alive` or `/ready` submit commands or call `/process`.
- Preserve Story 7.6A production auth semantics; do not use fake auth or dev HMAC tokens as DAPR service-invocation proof.
- Do not edit `Hexalith.EventStore` submodule files for this story.

### Out of Scope

- Production JWT/OIDC validation and auth smoke evidence; Story 7.6A owns it.
- Health and dependency readiness smoke tests; Story 7.6C owns that deployment slice.
- Pub/sub outage, drain recovery, and catch-up evidence; Story 7.6D owns it.
- Final deployment readiness checklist and evidence template publishing; Story 7.6E owns it.
- Vendor-specific Kubernetes, Helm, Azure Container Apps, Keycloak/Entra provisioning, mTLS certificate automation, or live production smoke execution.
- New dashboards, telemetry exporters, alert rules, or OpenTelemetry collector configuration.
- EventStore submodule changes, domain aggregate changes, query authorization changes, or cursor behavior changes.

### Testing Standards

- Use xUnit v3 and Shouldly; do not use `Assert.*` except existing xUnit skip mechanisms.
- Use `YamlDotNet` for YAML parsing; the Server.Tests project already references it.
- Keep deterministic configuration tests infrastructure-free and order-independent.
- Use existing `DaprFactAttribute`, `DaprTestPrerequisites`, `TenantsDaprTestFixture`, and `AspireTopologyFixture` for live DAPR/Aspire checks.
- Every smoke test must assert observable behavior: component contract, route policy, command result, event count, topic, safe diagnostic category, or skip reason.
- If VSTest cannot open sockets in this sandbox, build as needed and use the direct xUnit executable fallback already used in Stories 7.1 through 7.6A.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 7.6B: Validate DAPR Component and Service Invocation Smoke Tests`]
- [Source: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-31.md#Proposal B: Split Story 7.6 Before Handoff`]
- [Source: `_bmad-output/planning-artifacts/architecture.md#Requirements to Structure Mapping`]
- [Source: `_bmad-output/project-context.md#DAPR`]
- [Source: `_bmad-output/project-context.md#Testing Rules`]
- [Source: `_bmad-output/implementation-artifacts/7-1-provide-aspire-hosting-extensions-for-tenants.md`]
- [Source: `_bmad-output/implementation-artifacts/7-2-configure-dapr-components-for-local-and-production-deployment.md`]
- [Source: `_bmad-output/implementation-artifacts/7-5-prove-stateless-operation-health-and-startup-reconstruction.md`]
- [Source: `_bmad-output/implementation-artifacts/7-6a-validate-production-auth-smoke-tests.md`]
- [Source: `src/Hexalith.Tenants.AppHost/Program.cs`]
- [Source: `src/Hexalith.Tenants.Aspire/HexalithTenantsExtensions.cs`]
- [Source: `src/Hexalith.Tenants.Aspire/HexalithTenantsAspireOptions.cs`]
- [Source: `src/Hexalith.Tenants.AppHost/DaprComponents/statestore.yaml`]
- [Source: `src/Hexalith.Tenants.AppHost/DaprComponents/pubsub.yaml`]
- [Source: `src/Hexalith.Tenants.AppHost/DaprComponents/accesscontrol.yaml`]
- [Source: `deploy/dapr/accesscontrol.tenants.yaml`]
- [Source: `deploy/dapr/README.md`]
- [Source: `src/Hexalith.Tenants/appsettings.json`]
- [Source: `src/Hexalith.Tenants/Program.cs`]
- [Source: `tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/DaprEndToEndTests.cs`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/Fixtures/DaprFactAttribute.cs`]
- [Source: `tests/Hexalith.Tenants.IntegrationTests/Fixtures/AspireTopologyFixture.cs`]
- [Source: Microsoft Learn, Dapr integration for Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/community-toolkit/dapr)
- [Source: Microsoft Learn, Dapr integration for Aspire framework docs](https://learn.microsoft.com/en-us/dotnet/aspire/frameworks/dapr)
- [Source: DAPR Docs, Apply access control list configuration for service invocation](https://docs.dapr.io/operations/configuration/invoke-allowlist/)
- [Source: DAPR Docs, init CLI command reference](https://docs.dapr.io/reference/cli/dapr-init/)

## Project Structure Notes

- Alignment: Story 7.6B belongs in deterministic DAPR configuration tests, gated DAPR integration smoke tests, existing AppHost/Aspire fixtures where needed, and support-safe deployment evidence artifacts.
- Detected baseline: Story 7.2 already added production DAPR templates and deterministic YAML validation; Story 7.6B should package smoke evidence and close diagnostic gaps, not recreate configuration from scratch.
- Detected live-evidence boundary: `AspireTopologyFixture` proves AppHost process liveness and selected full-topology workflows when prerequisites exist; `TenantsDaprTestFixture` carries full DAPR actor/service-invocation evidence.
- Detected risk: local allow-by-default access control can mask production route-policy mistakes. Production deny-by-default YAML assertions and optional prepared-environment denial checks must remain explicit.
- No UI/frontend changes are required for this story.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-02: Reconciled Story 7.2, Story 7.5, and Story 7.6A completion notes. Confirmed 7.6B should extend deterministic DAPR/service-invocation smoke contracts and evidence, not duplicate auth, health/readiness, or pub/sub recovery stories.
- 2026-06-02: Confirmed AppHost resource names/AppIds remain `eventstore`, `tenants`, `eventstore-admin`, `eventstore-admin-ui`, and `sample`; component names remain `statestore`, `pubsub`, `tenants.events`, and `deadletter.tenants.events`; production Tenants receiver ACL remains deny-by-default with only `eventstore` allowed to `POST /process` and `POST /project`.
- 2026-06-02: Required focused `dotnet test` commands for Server and Integration projects aborted before execution with sandbox MSBuild/VSTest socket denial: `System.Net.Sockets.SocketException (13): Permission denied`. Treated as environment limitation, not product failure.
- 2026-06-02: Direct xUnit fallback passed for `EventPublicationConfigurationTests`: 20 total, 0 errors, 0 failed, 0 skipped.
- 2026-06-02: Direct xUnit fallback for `DaprEndToEndTests` and `AspireTopologyTests`: 22 total, 0 errors, 0 failed, 22 skipped. Exact skip reason: `DAPR integration prerequisites are unavailable. Run 'dapr init' and ensure Redis, placement, and scheduler are reachable.`
- 2026-06-02: Full direct xUnit regression sweep passed: Contracts 105/0 failed, Client 92/0 failed, Testing 181/0 failed, Sample 31/0 failed, Server 726/0 failed, Integration 218 total with 0 failed and 27 DAPR/performance prerequisite-gated skips.
- 2026-06-02: Debug test builds passed for Server.Tests and IntegrationTests with `MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0`, `--configuration Debug`, `--no-restore`, `-m:1`, `/nr:false`, and `/p:BuildInParallel=false`.
- 2026-06-02: QA generate-e2e-tests pass found and closed a diagnostics coverage gap: added deterministic tests for DAPR prerequisite skip messaging, fixture failure diagnostics, support-safe diagnostic output, and narrow infrastructure-startup classification.
- 2026-06-02: QA validation reran focused Debug builds successfully. Focused `dotnet test` commands remained blocked before execution by sandbox MSBuild/VSTest socket denial. Direct xUnit fallback passed `EventPublicationConfigurationTests`: 20 total, 0 failed; and `DaprEndToEndTests`, `AspireTopologyTests`, plus `DaprTestPrerequisiteDiagnosticsTests`: 32 total, 0 failed, 22 DAPR prerequisite-gated skips.
- 2026-06-02: Senior Developer Review found support-safe diagnostics still exposed raw `/process` exceptions through console output, HTTP problem details, and assertion messages. Auto-fixed by emitting command-category diagnostics, sanitizing DAPR startup skip details, and adding deterministic sanitizer coverage.
- 2026-06-02: Review validation: `dotnet test` focused Server.Tests and IntegrationTests builds succeeded but VSTest execution remained blocked by sandbox socket denial. Direct xUnit fallback passed `EventPublicationConfigurationTests`: 20 total, 0 failed; and `DaprTestPrerequisiteDiagnosticsTests`, `DaprEndToEndTests`, plus `StatelessRestartTests`: 31 total, 0 failed, 19 DAPR prerequisite-gated skips.

### Completion Notes List

- Extended deterministic DAPR configuration tests to assert stable AppHost resource names/AppIds, stable DAPR component names, actor state-store metadata, local/production scopes, dynamic DAPR sidecar port posture, receiver-specific deny-by-default service invocation, prerequisite diagnostic terms, and support-safe evidence constraints.
- Hardened live `CreateTenant` DAPR smoke coverage so, when DAPR prerequisites are available, the test proves one accepted aggregate actor command persists exactly one `TenantCreated` event and publishes exactly one matching event to `tenants.events`.
- Preserved existing `DaprFactAttribute`, `DaprTestPrerequisites`, `TenantsDaprTestFixture`, `AspireTopologyFixture`, and AppHost liveness-only boundaries; no new DAPR fixture, fixed DAPR sidecar ports, auth smoke duplication, health/readiness duplication, or pub/sub recovery duplication was introduced.
- Captured 7.6B evidence in `_bmad-output/implementation-artifacts/tests/test-summary.md`, separating static YAML/config/docs validation from live DAPR/AppHost smoke evidence.
- Live DAPR/AppHost tests were discoverable and correctly prerequisite-gated, but skipped in this sandbox because Redis, placement, and scheduler were unavailable. Static validation is not claimed as live deployment proof.
- Added QA-generated diagnostics tests so missing DAPR prerequisites and sidecar startup failures remain support-safe, category-specific, and do not mask product failures after prerequisites pass.
- Senior Developer Review auto-fixed raw exception leakage in DAPR fixture failure diagnostics. `/process` failures now report a support-safe command category, DAPR infrastructure startup skip messages pass through a sanitizer, and affected test assertion messages no longer embed raw exception bodies.

### Senior Developer Review (AI)

**Outcome:** Approved after auto-fix.

**Issues Found and Fixed**

- **HIGH:** DAPR `/process` failure diagnostics still emitted raw exception details via console output, HTTP problem details, and assertion messages. This violated AC1's support-safe diagnostics requirement because raw exception bodies can include payloads or environment details. Fixed in `TenantsDaprTestFixture` by retaining the exception internally but exposing only `LastProcessDiagnostic`, returning a support-safe problem detail, and logging command category only. Updated affected assertions in `DaprEndToEndTests` and `StatelessRestartTests`.
- **MEDIUM:** DAPR sidecar infrastructure skip messages carried raw sidecar stdout/stderr from startup exceptions. Fixed by passing startup failure text through `ToSupportSafeDiagnostic`, with deterministic tests covering JWT, bearer token, secret, connection string, and private-address redaction.

**Validation**

- `dotnet test tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~DaprTestPrerequisiteDiagnosticsTests|FullyQualifiedName~DaprEndToEndTests|FullyQualifiedName~StatelessRestartTests"`: build succeeded; VSTest aborted with sandbox `System.Net.Sockets.SocketException (13): Permission denied`.
- Direct xUnit fallback for `DaprTestPrerequisiteDiagnosticsTests`, `DaprEndToEndTests`, and `StatelessRestartTests`: 31 total, 0 failed, 19 skipped due unavailable DAPR prerequisites.
- `dotnet test tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj --configuration Debug --no-restore --filter "FullyQualifiedName~Configuration"`: build succeeded; VSTest aborted with sandbox `System.Net.Sockets.SocketException (13): Permission denied`.
- Direct xUnit fallback for `EventPublicationConfigurationTests`: 20 total, 0 failed, 0 skipped.

### File List

- `_bmad-output/implementation-artifacts/7-6b-validate-dapr-component-and-service-invocation-smoke-tests.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/DaprTestPrerequisiteDiagnosticsTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/Fixtures/TenantsDaprTestFixture.cs`
- `tests/Hexalith.Tenants.Server.Tests/Configuration/EventPublicationConfigurationTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/DaprEndToEndTests.cs`
- `tests/Hexalith.Tenants.IntegrationTests/StatelessRestartTests.cs`

## Change Log

| Date       | Version | Description | Author |
|------------|---------|-------------|--------|
| 2026-06-02 | 0.1     | Created Story 7.6B implementation context for DAPR component and service-invocation smoke tests. | GPT-5 Codex |
| 2026-06-02 | 1.0     | Added deterministic DAPR smoke-contract validation, hardened live CreateTenant service-invocation smoke assertions, captured support-safe evidence, and moved story to review. | GPT-5 Codex |
| 2026-06-02 | 1.1     | QA-generated diagnostics tests for DAPR prerequisite and startup-failure support-safe coverage. | GPT-5 Codex |
| 2026-06-02 | 1.2     | Senior Developer Review auto-fixed support-safe DAPR failure diagnostics and moved story to done. | GPT-5 Codex |
